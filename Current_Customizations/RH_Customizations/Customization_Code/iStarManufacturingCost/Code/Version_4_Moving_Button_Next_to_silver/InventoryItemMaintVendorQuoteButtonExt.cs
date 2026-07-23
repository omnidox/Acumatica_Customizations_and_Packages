using System;
using System.Reflection;
using System.Web;
using System.Web.UI;
using PX.Data;
using PX.Objects.IN;
using PX.Web.UI;

namespace iStarCostCalculationExtensions
{
    /// <summary>
    /// Adds a runtime Calc button beside the Silver, Grams field on IN202500.
    ///
    /// The button invokes the existing CalculateVendorQuote PXAction supplied
    /// by the vendor-quote costing graph extension.
    ///
    /// The PXButton is added to valueParametersPanel on the server so Acumatica
    /// can configure and process its callback normally. After the page renders,
    /// JavaScript:
    ///
    /// 1. Hides only the standard Calculate Vendor Quote toolbar item.
    /// 2. Determines whether UsrActualGRAMSilver is currently visible.
    /// 3. Hides the runtime Calc button when the Silver field is hidden.
    /// 4. Moves the runtime Calc button beside the Silver field when visible.
    ///
    /// The PXAction itself remains visible and registered so the runtime button
    /// can continue invoking CalculateVendorQuote.
    /// </summary>
    public class InventoryItemMaintVendorQuoteButtonExt
        : PXGraphExtension<InventoryItemMaint>
    {
        private const string TracePrefix =
            "[VendorQuoteButton]";

        private const string ExpectedScreenID =
            "IN202500";

        private const string TargetPanelID =
            "valueParametersPanel";

        /*
         * This is the generated ASPX control ID currently assigned to
         * UsrActualGRAMSilver.
         *
         * Because generated IDs can change when the vendor customization is
         * republished, FindSilverControl also includes a DataField fallback.
         */
        private const string GeneratedSilverControlID =
            "CstPXNumberEdit47";

        private const string SilverDataField =
            "UsrActualGRAMSilver";

        private const string DataSourceID =
            "ds";

        private const string RuntimeButtonID =
            "btnCalculateVendorQuoteRuntime";

        private const string ActionName =
            "CalculateVendorQuote";

        /*
         * Acumatica renders the standard toolbar action with this lower-camel
         * data-cmd value:
         *
         *     <li data-cmd="calculateVendorQuote">
         *
         * The runtime PXButton beside Silver, Grams is not this toolbar <li>,
         * so the selector can hide the toolbar presentation independently.
         */
        private const string ToolbarCommandName =
            "calculateVendorQuote";

        private const string HookRegistrationKey =
            "VendorQuoteButton.PageEventsRegistered";

        private const string InjectionCompletedKey =
            "VendorQuoteButton.InjectionCompleted";

        private const string RelocationScriptKey =
            "VendorQuoteButton.MoveBesideSilver";

        public static bool IsActive()
        {
            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            try
            {
                Page page =
                    GetActivePage();

                if (page == null ||
                    !IsExpectedScreen(page))
                {
                    return;
                }

                HttpContext context =
                    HttpContext.Current;

                if (context == null)
                {
                    PXTrace.WriteWarning(
                        $"{TracePrefix} No active HttpContext was available.");

                    return;
                }

                /*
                 * The graph extension may be initialized more than once during
                 * the same request. Register the page event only once.
                 */
                if (context.Items[HookRegistrationKey] != null)
                {
                    return;
                }

                context.Items[HookRegistrationKey] =
                    true;

                /*
                 * LoadComplete has been verified as a safe point for this page.
                 * Attempting to modify the control tree during Initialize can
                 * interfere with Acumatica's page and view initialization.
                 */
                page.LoadComplete +=
                    Page_LoadComplete;

                PXTrace.WriteInformation(
                    $"{TracePrefix} Registered LoadComplete handler. " +
                    $"PageType={page.GetType().FullName}");
            }
            catch (Exception ex)
            {
                PXTrace.WriteError(
                    $"{TracePrefix} Initialize failed. {ex}");
            }
        }

        private void Page_LoadComplete(
            object sender,
            EventArgs e)
        {
            try
            {
                Page page =
                    sender as Page;

                if (page == null)
                {
                    PXTrace.WriteWarning(
                        $"{TracePrefix} LoadComplete sender was not a Page.");

                    return;
                }

                TryInjectAndPositionButton(
                    page);
            }
            catch (Exception ex)
            {
                PXTrace.WriteError(
                    $"{TracePrefix} LoadComplete failed. {ex}");
            }
        }

        private void TryInjectAndPositionButton(
            Page page)
        {
            if (page == null ||
                !IsExpectedScreen(page))
            {
                return;
            }

            HttpContext context =
                HttpContext.Current;

            Control targetPanel =
                ControlHelper.FindControl(
                    TargetPanelID,
                    page);

            if (targetPanel == null)
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} Target panel '{TargetPanelID}' was not found.");

                return;
            }

            Control silverControl =
                FindSilverControl(
                    page);

            if (silverControl == null)
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} Silver control was not found. " +
                    $"ExpectedControlID={GeneratedSilverControlID}; " +
                    $"ExpectedDataField={SilverDataField}.");

                return;
            }

            PXDataSource dataSource =
                ControlHelper.FindControl(
                    DataSourceID,
                    page) as PXDataSource;

            if (dataSource == null)
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} PXDataSource '{DataSourceID}' was not found.");

                return;
            }

            Control existingButton =
                FindImmediateChild(
                    targetPanel,
                    RuntimeButtonID);

            /*
             * If another initialization path already added the button during
             * this request, do not create a duplicate. Register the relocation
             * script again so the rendered element is still positioned or
             * hidden according to the Silver field visibility.
             */
            if (existingButton != null)
            {
                RegisterButtonRelocationScript(
                    page,
                    silverControl,
                    existingButton);

                MarkInjectionCompleted(
                    context);

                return;
            }

            if (context != null &&
                context.Items[InjectionCompletedKey] is bool completed &&
                completed)
            {
                return;
            }

            PXButton calcButton =
                CreateCalcButton();

            /*
             * Add the PXButton to an actual Acumatica server-side container.
             * This allows Acumatica to render the control and process its native
             * callback to the existing CalculateVendorQuote PXAction.
             */
            targetPanel.Controls.Add(
                calcButton);

            /*
             * Move or hide only the rendered browser element.
             *
             * The server-side PXButton remains a child of
             * valueParametersPanel.
             */
            RegisterButtonRelocationScript(
                page,
                silverControl,
                calcButton);

            MarkInjectionCompleted(
                context);

            PXTrace.WriteInformation(
                $"{TracePrefix} Runtime Calc button added. " +
                $"Panel={DescribeControl(targetPanel)}; " +
                $"Silver={DescribeControl(silverControl)}; " +
                $"Button={DescribeControl(calcButton)}; " +
                $"Action={ActionName}; " +
                $"ToolbarCommand={ToolbarCommandName}");
        }

        private static PXButton CreateCalcButton()
        {
            PXButton button =
                new PXButton
                {
                    ID = RuntimeButtonID,
                    Text = "Calc",
                    ToolTip = "Calculate Vendor Quote",
                    CausesValidation = true
                };

            button.AutoCallBack.Enabled =
                true;

            button.AutoCallBack.Target =
                DataSourceID;

            button.AutoCallBack.Command =
                ActionName;

            return button;
        }

        private static Control FindSilverControl(
            Page page)
        {
            if (page == null)
            {
                return null;
            }

            /*
             * Prefer the exact generated ASPX control ID currently known to
             * represent UsrActualGRAMSilver.
             */
            Control silverControl =
                ControlHelper.FindControl(
                    GeneratedSilverControlID,
                    page);

            if (silverControl != null)
            {
                return silverControl;
            }

            /*
             * Fall back to DataField in case a future publish changes the
             * generated control ID.
             */
            return FindControlByDataField(
                page,
                SilverDataField);
        }

        private static Control FindControlByDataField(
            Control root,
            string dataField)
        {
            if (root == null ||
                string.IsNullOrWhiteSpace(dataField))
            {
                return null;
            }

            string currentDataField =
                TryGetDataField(
                    root);

            if (string.Equals(
                currentDataField,
                dataField,
                StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            foreach (Control child in root.Controls)
            {
                Control match =
                    FindControlByDataField(
                        child,
                        dataField);

                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void RegisterButtonRelocationScript(
            Page page,
            Control silverControl,
            Control calcButton)
        {
            if (page == null ||
                silverControl == null ||
                calcButton == null)
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} Relocation script was not registered because " +
                    "one or more required controls were unavailable.");

                return;
            }

            string rawSilverClientID =
                silverControl.ClientID;

            string rawButtonClientID =
                calcButton.ClientID;

            if (string.IsNullOrWhiteSpace(rawSilverClientID) ||
                string.IsNullOrWhiteSpace(rawButtonClientID))
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} Relocation script was not registered because " +
                    "one or more ClientIDs were empty. " +
                    $"SilverClientID={rawSilverClientID ?? "<null>"}; " +
                    $"ButtonClientID={rawButtonClientID ?? "<null>"}");

                return;
            }

            string silverClientID =
                HttpUtility.JavaScriptStringEncode(
                    rawSilverClientID);

            string buttonClientID =
                HttpUtility.JavaScriptStringEncode(
                    rawButtonClientID);

            string toolbarCommandName =
                HttpUtility.JavaScriptStringEncode(
                    ToolbarCommandName);

            string script =
                $@"
(function () {{
    var silverClientID = '{silverClientID}';
    var buttonClientID = '{buttonClientID}';
    var toolbarCommandName = '{toolbarCommandName}';

    var attemptCount = 0;
    var maximumAttempts = 40;
    var retryDelayMilliseconds = 100;

    function closestElement(element, selector) {{
        if (!element) {{
            return null;
        }}

        if (typeof element.closest === 'function') {{
            return element.closest(selector);
        }}

        var current = element;

        while (current) {{
            var matches =
                current.matches ||
                current.msMatchesSelector ||
                current.webkitMatchesSelector;

            if (matches &&
                matches.call(current, selector)) {{
                return current;
            }}

            current = current.parentElement;
        }}

        return null;
    }}

    /*
     * Hides only the standard Acumatica toolbar representation of the
     * CalculateVendorQuote action.
     *
     * Example rendered element:
     *
     *     <li class=""qp-tool-bar-item""
     *         data-cmd=""calculateVendorQuote"">
     *
     * This does not hide the PXAction itself and does not target the runtime
     * qp-button injected beside Silver, Grams.
     */
    function hideToolbarVendorQuoteButton() {{
        var selector =
            'li.qp-tool-bar-item[data-cmd=""' +
            toolbarCommandName +
            '""]';

        var toolbarButtons =
            document.querySelectorAll(selector);

        if (!toolbarButtons ||
            toolbarButtons.length === 0) {{
            return false;
        }}

        for (var index = 0;
             index < toolbarButtons.length;
             index++) {{
            var toolbarButton =
                toolbarButtons[index];

            if (!toolbarButton) {{
                continue;
            }}

            toolbarButton.style.display =
                'none';

            toolbarButton.setAttribute(
                'aria-hidden',
                'true');

            toolbarButton.setAttribute(
                'data-istar-hidden-toolbar-action',
                'true');
        }}

        return true;
    }}

    /*
     * Determines whether an element is actually visible in the rendered page.
     *
     * The Silver server control may still exist for non-Silver items even when
     * Acumatica hides its rendered field. Therefore, control existence alone
     * cannot determine whether the Calc button should be shown.
     */
    function elementIsVisible(element) {{
        if (!element ||
            !document.documentElement.contains(element)) {{
            return false;
        }}

        var current =
            element;

        while (current &&
               current !== document.documentElement) {{
            var style =
                window.getComputedStyle
                    ? window.getComputedStyle(current)
                    : current.currentStyle;

            if (style) {{
                if (style.display === 'none' ||
                    style.visibility === 'hidden' ||
                    style.visibility === 'collapse') {{
                    return false;
                }}
            }}

            if (current.hidden === true ||
                current.getAttribute('aria-hidden') === 'true') {{
                return false;
            }}

            current =
                current.parentElement;
        }}

        /*
         * A field hidden by one of its ancestors normally has no rendered
         * client rectangles.
         */
        if (typeof element.getClientRects === 'function' &&
            element.getClientRects().length === 0) {{
            return false;
        }}

        return true;
    }}

    function moveButton() {{
        attemptCount++;

        /*
         * Hide only the standard toolbar item.
         *
         * This is deliberately performed on every attempt because Acumatica's
         * toolbar may finish rendering after the remainder of the page.
         */
        hideToolbarVendorQuoteButton();

        var silverInput =
            document.getElementById(silverClientID);

        var innerButton =
            document.getElementById(buttonClientID);

        if (!silverInput ||
            !innerButton) {{
            return false;
        }}

        var silverEditor =
            closestElement(
                silverInput,
                'qp-number-editor');

        var editorContainer =
            closestElement(
                silverInput,
                '.fld-c');

        /*
         * The ClientID belongs to the inner native button. Move or hide the
         * outer qp-button so Acumatica's component remains intact.
         */
        var buttonHost =
            closestElement(
                innerButton,
                'qp-button') ||
            innerButton;

        if (!silverEditor ||
            !editorContainer ||
            !buttonHost) {{
            return false;
        }}

        /*
         * The Silver control can exist in the HTML for every item even though
         * Acumatica hides it for non-Silver items.
         *
         * Hide the Calc button whenever the actual Silver editor is not
         * visible.
         *
         * This visibility check must occur before editorContainer is changed
         * to display:flex. Otherwise this script could accidentally reveal a
         * container that Acumatica intentionally hid.
         */
        var silverFieldIsVisible =
            elementIsVisible(silverInput) &&
            elementIsVisible(silverEditor) &&
            elementIsVisible(editorContainer);

        if (!silverFieldIsVisible) {{
            buttonHost.style.display =
                'none';

            buttonHost.setAttribute(
                'aria-hidden',
                'true');

            buttonHost.setAttribute(
                'data-istar-vendor-quote-button-applicable',
                'false');

            buttonHost.removeAttribute(
                'data-istar-vendor-quote-button-positioned');

            return true;
        }}

        /*
         * The field is visible, so the runtime Calc button is applicable.
         */
        buttonHost.removeAttribute(
            'aria-hidden');

        buttonHost.setAttribute(
            'data-istar-vendor-quote-button-applicable',
            'true');

        /*
         * Place the Silver editor and runtime Calc button on the same
         * horizontal row.
         */
        editorContainer.style.display =
            'flex';

        editorContainer.style.alignItems =
            'center';

        editorContainer.style.flexWrap =
            'nowrap';

        silverEditor.style.flex =
            '0 1 auto';

        buttonHost.style.display =
            'inline-block';

        buttonHost.style.flex =
            '0 0 auto';

        buttonHost.style.marginLeft =
            '8px';

        buttonHost.style.verticalAlign =
            'middle';

        /*
         * Acumatica may initially render the tooltip or action caption instead
         * of the compact button caption. Normalize it to Calc.
         */
        var textElement =
            buttonHost.querySelector(
                '.btn-inner .text');

        if (textElement) {{
            textElement.textContent =
                'Calc';
        }}

        /*
         * Move the rendered qp-button immediately after the Silver editor.
         */
        if (buttonHost.parentElement !== editorContainer ||
            silverEditor.nextElementSibling !== buttonHost) {{
            if (silverEditor.nextSibling) {{
                editorContainer.insertBefore(
                    buttonHost,
                    silverEditor.nextSibling);
            }}
            else {{
                editorContainer.appendChild(
                    buttonHost);
            }}
        }}

        buttonHost.setAttribute(
            'data-istar-vendor-quote-button-positioned',
            'true');

        return true;
    }}

    function attemptMove() {{
        if (moveButton()) {{
            return;
        }}

        /*
         * Acumatica's client-side components may finish rendering shortly
         * after the ASP.NET startup script executes, so retry for up to
         * approximately four seconds.
         */
        if (attemptCount < maximumAttempts) {{
            window.setTimeout(
                attemptMove,
                retryDelayMilliseconds);
        }}
        else if (window.console &&
                 typeof window.console.warn === 'function') {{
            window.console.warn(
                '[VendorQuoteButton] Unable to position or hide the Calc ' +
                'button after ' +
                maximumAttempts +
                ' attempts.');
        }}
    }}

    attemptMove();
}})();";

            ScriptManager.RegisterStartupScript(
                page,
                page.GetType(),
                RelocationScriptKey,
                script,
                true);

            PXTrace.WriteInformation(
                $"{TracePrefix} Registered Calc button relocation, field " +
                $"visibility, and toolbar-hiding script. " +
                $"SilverClientID={rawSilverClientID}; " +
                $"ButtonClientID={rawButtonClientID}; " +
                $"ToolbarCommand={ToolbarCommandName}");
        }

        private static Control FindImmediateChild(
            Control parent,
            string childID)
        {
            if (parent == null ||
                string.IsNullOrWhiteSpace(childID))
            {
                return null;
            }

            foreach (Control child in parent.Controls)
            {
                if (string.Equals(
                    child.ID,
                    childID,
                    StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static string TryGetDataField(
            Control control)
        {
            if (control == null)
            {
                return null;
            }

            try
            {
                PropertyInfo property =
                    control.GetType().GetProperty(
                        "DataField",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                if (property == null ||
                    !property.CanRead ||
                    property.PropertyType != typeof(string))
                {
                    return null;
                }

                return
                    property.GetValue(
                        control,
                        null) as string;
            }
            catch
            {
                return null;
            }
        }

        private static Page GetActivePage()
        {
            HttpContext context =
                HttpContext.Current;

            if (context == null)
            {
                return null;
            }

            return
                context.Handler as Page;
        }

        private static bool IsExpectedScreen(
            Page page)
        {
            string currentScreenID =
                PXSiteMap.CurrentScreenID;

            if (!string.IsNullOrWhiteSpace(currentScreenID))
            {
                string normalizedScreenID =
                    currentScreenID
                        .Replace(".", string.Empty)
                        .Replace("-", string.Empty)
                        .Trim();

                return string.Equals(
                    normalizedScreenID,
                    ExpectedScreenID,
                    StringComparison.OrdinalIgnoreCase);
            }

            string path =
                page?.Request?.AppRelativeCurrentExecutionFilePath;

            if (string.IsNullOrWhiteSpace(path))
            {
                path =
                    page?.Request?.Path;
            }

            return
                !string.IsNullOrWhiteSpace(path) &&
                path.IndexOf(
                    ExpectedScreenID,
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void MarkInjectionCompleted(
            HttpContext context)
        {
            if (context != null)
            {
                context.Items[InjectionCompletedKey] =
                    true;
            }
        }

        private static string DescribeControl(
            Control control)
        {
            if (control == null)
            {
                return "<null>";
            }

            string dataField =
                TryGetDataField(
                    control);

            return
                $"ID={control.ID ?? "<null>"}," +
                $"Type={control.GetType().FullName}," +
                $"DataField={dataField ?? "<none>"}," +
                $"ParentID={control.Parent?.ID ?? "<none>"}";
        }
    }
}