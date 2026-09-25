using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AA.Objects.Labels;
using PX.Data;
using PX.Objects.CR;

namespace AA.Objects.AL.Integration.PerPackage
{
    /// <summary>
    /// ========================================================================
    /// REMEMBERED LABEL MODEL PER CUSTOMER / USER / MASTER-CARTON FLAG
    /// ========================================================================
    ///
    /// ResolveModelIdByAsgardRules evaluates every package model's rules. With
    /// the current rules the answer depends only on:
    /// - Document.CustomerID.AcctName   (customer rules such as CustomerIsTarget)
    /// - UsrIsParentBox                 (the "-is-Master" print rules)
    /// - the current user                (printer eligibility)
    ///
    /// so the resolved model can be reused for every later carton with the same
    /// customer, user, and master-carton flag.
    ///
    /// Safety:
    /// - The result is only remembered when EVERY candidate rule reads nothing
    ///   except those fields (IsCacheSafeRule). A new rule on any other field
    ///   automatically disables reuse until that rule is removed.
    /// - The slot is a PXDatabase slot: Acumatica clears it whenever ALRule,
    ///   ALModelPrinter, or BAccount (customer names) change, and it is kept
    ///   separately per tenant.
    /// - ALModel is deliberately NOT a dependent table: Asgard updates the printed
    ///   model's ALModel row on every print (AcuLabelContext.RenderAsOutput), which
    ///   would clear the slot after every label. Instead the key contains a
    ///   fingerprint of the model fields that affect resolution (BuildModelFingerprint),
    ///   so activating, deactivating, or re-ruling a model produces a new key.
    /// </summary>
    internal sealed class ModelResolutionCache
    {
        private static readonly Type[] DependentTables =
        {
            typeof(ALRule),
            typeof(ALModelPrinter),
            typeof(BAccount)
        };

        /// <summary>Field references the key accounts for.</summary>
        private static readonly HashSet<string> CacheSafeReferences =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Document.CustomerID.AcctName",
                "Packages.UsrIsParentBox",
                "ALPackages.UsrIsParentBox",
                "ALiStarPackages.UsrIsParentBox"
            };

        /// <summary>Scriban built-in function objects, e.g. string.Contains.</summary>
        private static readonly string[] BuiltInPrefixes =
        {
            "string.", "math.", "array.", "date.", "object.", "regex.", "timespan.", "html."
        };

        /// <summary>Scriban keywords and literals that may appear alone.</summary>
        private static readonly HashSet<string> Keywords =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "true", "false", "null", "empty", "and", "or", "not"
            };

        private static readonly Regex StringLiteral =
            new Regex("'[^']*'|\"[^\"]*\"", RegexOptions.Compiled);

        private static readonly Regex Reference =
            new Regex(@"[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*", RegexOptions.Compiled);

        private readonly ConcurrentDictionary<string, Guid> _modelByKey =
            new ConcurrentDictionary<string, Guid>();

        private static ModelResolutionCache Slot =>
            PXDatabase.GetSlot<ModelResolutionCache>(
                typeof(ModelResolutionCache).FullName,
                DependentTables);

        public static string BuildKey(
            int? customerID,
            Guid userID,
            bool isParentBox,
            IEnumerable<ALModel> candidateModels)
        {
            return $"{customerID}|{userID}|{isParentBox}|{BuildModelFingerprint(candidateModels)}";
        }

        /// <summary>
        /// Hash of the candidate model fields that decide which model is resolved:
        /// the set of active package models, their rules, reverse flags, and the
        /// Description used for the UCC tie-break.
        /// </summary>
        private static string BuildModelFingerprint(IEnumerable<ALModel> candidateModels)
        {
            string fields = string.Join(
                "\n",
                candidateModels
                    .OrderBy(m => m.LabelID)
                    .Select(m => string.Join(
                        "|",
                        m.LabelID,
                        m.BasedOnView,
                        m.FilterRuleID,
                        m.ReverseFilter,
                        m.PrintRuleID,
                        m.ReversePrint,
                        m.Description)));

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(fields));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        public static bool TryGet(string key, out Guid modelId)
        {
            return Slot._modelByKey.TryGetValue(key, out modelId);
        }

        public static void Store(string key, Guid modelId)
        {
            Slot._modelByKey[key] = modelId;
        }

        /// <summary>
        /// True when the rule is missing or empty, or its expression references
        /// only the fields that are part of the cache key.
        /// </summary>
        public static bool IsCacheSafeRule(ALRule rule)
        {
            string expression = rule?.Expression;

            if (string.IsNullOrWhiteSpace(expression))
                return true;

            string withoutLiterals = StringLiteral.Replace(expression, " ");

            foreach (Match match in Reference.Matches(withoutLiterals))
            {
                string reference = match.Value;

                if (CacheSafeReferences.Contains(reference) ||
                    Keywords.Contains(reference) ||
                    BuiltInPrefixes.Any(prefix =>
                        reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
