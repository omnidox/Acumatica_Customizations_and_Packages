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
    /// Adds a runtime Calc button beside the Unit Cost field on IN202500.
    ///
    /// The button invokes the existing CalculateVendorQuote PXAction supplied
    /// by the vendor-quote costing graph extension.
    ///
    /// The PXButton is added to valueParametersPanel on the server so Acumatica
    /// can configure and process its callback normally.
    ///
    /// After the page renders, JavaScript:
    ///
    /// 1. Identifies the injected runtime Calc button.
    /// 2. Hides only the standard Acumatica toolbar representation of
    ///    CalculateVendorQuote.
    /// 3. Determines whether UsrActualGRAMSilver is currently visible.
    /// 4. Hides the runtime Calc button when the Silver field is hidden.
    /// 5. Moves the runtime Calc button beside the Unit Cost field.
    /// 6. Mirrors the enabled or disabled state of the underlying
    ///    CalculateVendorQuote PXAction.
    ///
    /// The Silver field determines whether the button is applicable.
    /// The Unit Cost field determines where the button is positioned.
    ///
    /// The CalculateVendorQuote PXAction remains visible and registered so the
    /// injected runtime button can continue invoking it.
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
         * Generated ASPX control ID currently assigned to Unit Cost.
         *
         * FindUnitCostControl also includes a DataField fallback in case
         * the generated control ID changes after publishing.
         */
        private const string GeneratedUnitCostControlID =
            "CstPXNumberEdit52";

        private const string UnitCostDataField =
            "UsrUnitCost";

        /*
         * Generated ASPX control ID currently assigned to
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
         * Acumatica currently renders the standard toolbar item using an ID
         * ending with:
         *
         *     _ds_ToolBar_calculateVendorQuote
         *
         * This suffix distinguishes the toolbar item from the separately
         * injected runtime PXButton.
         */
        private const string ToolbarElementIDSuffix =
            "_ds_ToolBar_calculateVendorQuote";

        private const string HookRegistrationKey =
            "VendorQuoteButton.PageEventsRegistered";

        private const string InjectionCompletedKey =
            "VendorQuoteButton.InjectionCompleted";

        private const string RelocationScriptKey =
            "VendorQuoteButton.MoveBesideUnitCost";

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
                 *
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
                    $"{TracePrefix} Target panel '{TargetPanelID}' " +
                    "was not found.");

                return;
            }

            /*
             * The Silver control determines whether the vendor-quote
             * calculation is applicable to the current stock item.
             */
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

            /*
             * The Unit Cost control is the visual positioning target.
             *
             * The runtime Calc button is moved beside this field.
             */
            Control unitCostControl =
                FindUnitCostControl(
                    page);

            if (unitCostControl == null)
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} Unit Cost control was not found. " +
                    $"ExpectedControlID={GeneratedUnitCostControlID}; " +
                    $"ExpectedDataField={UnitCostDataField}.");

                return;
            }

            PXDataSource dataSource =
                ControlHelper.FindControl(
                    DataSourceID,
                    page) as PXDataSource;

            if (dataSource == null)
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} PXDataSource '{DataSourceID}' " +
                    "was not found.");

                return;
            }

            Control existingButton =
                FindImmediateChild(
                    targetPanel,
                    RuntimeButtonID);

            /*
             * If another initialization path already added the button during
             * this request, do not create a duplicate.
             *
             * Register the browser script again so:
             *
             * - Silver applicability is reevaluated;
             * - the button is moved beside Unit Cost;
             * - the runtime button mirrors the PXAction enabled state;
             * - the standard toolbar action remains hidden.
             */
            if (existingButton != null)
            {
                RegisterButtonRelocationScript(
                    page,
                    silverControl,
                    unitCostControl,
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
             *
             * This allows Acumatica to render the control and process its
             * native callback to the existing CalculateVendorQuote PXAction.
             */
            targetPanel.Controls.Add(
                calcButton);

            /*
             * Move or hide only the rendered browser element.
             *
             * The server-side PXButton remains a child of
             * valueParametersPanel.
             *
             * Silver determines applicability.
             * Unit Cost determines placement.
             */
            RegisterButtonRelocationScript(
                page,
                silverControl,
                unitCostControl,
                calcButton);

            MarkInjectionCompleted(
                context);

            PXTrace.WriteInformation(
                $"{TracePrefix} Runtime Calc button added. " +
                $"Panel={DescribeControl(targetPanel)}; " +
                $"SilverApplicabilityControl=" +
                $"{DescribeControl(silverControl)}; " +
                $"UnitCostPositionControl=" +
                $"{DescribeControl(unitCostControl)}; " +
                $"Button={DescribeControl(calcButton)}; " +
                $"Action={ActionName}; " +
                $"ToolbarIDSuffix={ToolbarElementIDSuffix}");
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

        private static Control FindUnitCostControl(
            Page page)
        {
            if (page == null)
            {
                return null;
            }

            /*
             * Prefer the exact generated ASPX control ID currently known to
             * represent Unit Cost.
             */
            Control unitCostControl =
                ControlHelper.FindControl(
                    GeneratedUnitCostControlID,
                    page);

            if (unitCostControl != null)
            {
                return unitCostControl;
            }

            /*
             * Fall back to DataField in case a future publish changes the
             * generated control ID.
             */
            return FindControlByDataField(
                page,
                UnitCostDataField);
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
            Control unitCostControl,
            Control calcButton)
        {
            if (page == null ||
                silverControl == null ||
                unitCostControl == null ||
                calcButton == null)
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} Relocation script was not registered " +
                    "because one or more required controls were unavailable.");

                return;
            }

            string rawSilverClientID =
                silverControl.ClientID;

            string rawUnitCostClientID =
                unitCostControl.ClientID;

            string rawButtonClientID =
                calcButton.ClientID;

            if (string.IsNullOrWhiteSpace(rawSilverClientID) ||
                string.IsNullOrWhiteSpace(rawUnitCostClientID) ||
                string.IsNullOrWhiteSpace(rawButtonClientID))
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} Relocation script was not registered " +
                    "because one or more ClientIDs were empty. " +
                    $"SilverClientID={rawSilverClientID ?? "<null>"}; " +
                    $"UnitCostClientID={rawUnitCostClientID ?? "<null>"}; " +
                    $"ButtonClientID={rawButtonClientID ?? "<null>"}");

                return;
            }

            string silverClientID =
                HttpUtility.JavaScriptStringEncode(
                    rawSilverClientID);

            string unitCostClientID =
                HttpUtility.JavaScriptStringEncode(
                    rawUnitCostClientID);

            string buttonClientID =
                HttpUtility.JavaScriptStringEncode(
                    rawButtonClientID);

            string toolbarElementIDSuffix =
                HttpUtility.JavaScriptStringEncode(
                    ToolbarElementIDSuffix);

            string script =
                $@"
(function () {{
    var silverClientID = '{silverClientID}';
    var unitCostClientID = '{unitCostClientID}';
    var buttonClientID = '{buttonClientID}';
    var toolbarElementIDSuffix = '{toolbarElementIDSuffix}';

    var attemptCount = 0;
    var maximumAttempts = 40;
    var retryDelayMilliseconds = 100;

    /*
     * Stores the runtime button host after it has been positively identified.
     *
     * This is used as an additional safeguard to ensure the toolbar-hiding
     * logic never hides the runtime button or one of its ancestors.
     */
    var identifiedRuntimeButtonHost = null;

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
     * Determines whether an element is actually visible in the rendered page.
     *
     * The Silver server control may still exist for non-Silver items even when
     * Acumatica hides its rendered field. Therefore, control existence alone
     * cannot determine whether the runtime Calc button should be shown.
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

    /*
     * Determines whether an action element is disabled.
     *
     * Acumatica may express the disabled state through:
     *
     * - the native disabled property;
     * - a disabled attribute;
     * - aria-disabled;
     * - a CSS class containing disabled;
     * - or a nested button element.
     */
    function elementIsDisabled(element) {{
        if (!element) {{
            return false;
        }}

        if (element.disabled === true ||
            element.getAttribute('disabled') !== null ||
            element.getAttribute('aria-disabled') === 'true') {{
            return true;
        }}

        var className =
            typeof element.className === 'string'
                ? element.className.toLowerCase()
                : '';

        if (className.indexOf('disabled') >= 0) {{
            return true;
        }}

        var nestedControl =
            element.querySelector(
                'button, input, [role=""button""]');

        if (nestedControl &&
            nestedControl !== element) {{
            return elementIsDisabled(
                nestedControl);
        }}

        return false;
    }}

    function getToolbarVendorQuoteButton() {{
        var selector =
            '[id$=""' +
            toolbarElementIDSuffix +
            '""]';

        return document.querySelector(
            selector);
    }}

    /*
     * Reads the enabled state from the standard toolbar rendering of the
     * CalculateVendorQuote PXAction.
     *
     * The toolbar item is used as a client-side representation of the state
     * established by CalculateVendorQuote.SetEnabled on the server.
     */
    function isVendorQuoteActionDisabled() {{
        var toolbarElement =
            getToolbarVendorQuoteButton();

        /*
         * When the toolbar element has not rendered yet, avoid incorrectly
         * disabling the runtime button. The retry loop will reevaluate the
         * state after Acumatica finishes rendering the toolbar.
         */
        if (!toolbarElement) {{
            return false;
        }}

        return elementIsDisabled(
            toolbarElement);
    }}

    /*
     * Hides only the action rendered in the primary PXDataSource toolbar.
     *
     * The toolbar element observed on IN202500 has an ID similar to:
     *
     *     ctl00_phDS_ds_ToolBar_calculateVendorQuote
     *
     * Therefore, the selector targets only an element whose ID ends with:
     *
     *     _ds_ToolBar_calculateVendorQuote
     *
     * This is intentionally more specific than selecting by data-cmd because
     * multiple rendered presentations can be associated with the same action.
     */
    function hideToolbarVendorQuoteButton() {{
        var toolbarElement =
            getToolbarVendorQuoteButton();

        if (!toolbarElement) {{
            return false;
        }}

        /*
         * Re-resolve the runtime button in case Acumatica replaced part of the
         * DOM during a callback.
         */
        var runtimeInnerButton =
            document.getElementById(
                buttonClientID);

        var runtimeButtonHost =
            closestElement(
                runtimeInnerButton,
                'qp-button') ||
            runtimeInnerButton ||
            identifiedRuntimeButtonHost;

        /*
         * Never hide anything that is, contains, or is contained by the
         * positively identified runtime Calc button.
         */
        if (runtimeButtonHost &&
            (toolbarElement === runtimeButtonHost ||
             toolbarElement.contains(runtimeButtonHost) ||
             runtimeButtonHost.contains(toolbarElement))) {{
            return false;
        }}

        toolbarElement.style.setProperty(
            'display',
            'none',
            'important');

        toolbarElement.setAttribute(
            'aria-hidden',
            'true');

        toolbarElement.setAttribute(
            'data-istar-hidden-toolbar-action',
            'true');

        return true;
    }}

    /*
     * Applies the CalculateVendorQuote PXAction state to the runtime button.
     *
     * The server-side action validation remains authoritative. This function
     * only ensures that the runtime button visually and behaviorally reflects
     * the state presented by Acumatica in the toolbar.
     */
    function applyRuntimeButtonEnabledState(
        buttonHost,
        innerButton) {{
        if (!buttonHost ||
            !innerButton) {{
            return;
        }}

        var actionIsDisabled =
            isVendorQuoteActionDisabled();

        var runtimeNativeButton =
            buttonHost.querySelector(
                'button, input[type=""button""], [role=""button""]') ||
            innerButton;

        if (actionIsDisabled) {{
            buttonHost.setAttribute(
                'aria-disabled',
                'true');

            buttonHost.setAttribute(
                'data-istar-vendor-quote-button-enabled',
                'false');

            buttonHost.classList.add(
                'disabled');

            /*
             * Prevent a click from reaching the runtime control even if the
             * rendered Acumatica component does not use a native button.
             */
            buttonHost.style.pointerEvents =
                'none';

            buttonHost.style.cursor =
                'not-allowed';

            buttonHost.style.opacity =
                '0.55';

            if (runtimeNativeButton) {{
                runtimeNativeButton.disabled =
                    true;

                runtimeNativeButton.setAttribute(
                    'disabled',
                    'disabled');

                runtimeNativeButton.setAttribute(
                    'aria-disabled',
                    'true');

                runtimeNativeButton.tabIndex =
                    -1;
            }}

            return;
        }}

        buttonHost.removeAttribute(
            'aria-disabled');

        buttonHost.setAttribute(
            'data-istar-vendor-quote-button-enabled',
            'true');

        buttonHost.classList.remove(
            'disabled');

        buttonHost.style.removeProperty(
            'pointer-events');

        buttonHost.style.removeProperty(
            'cursor');

        buttonHost.style.removeProperty(
            'opacity');

        if (runtimeNativeButton) {{
            runtimeNativeButton.disabled =
                false;

            runtimeNativeButton.removeAttribute(
                'disabled');

            runtimeNativeButton.removeAttribute(
                'aria-disabled');

            /*
             * Remove only the tab index introduced by this script.
             * Acumatica may subsequently restore its normal tab order.
             */
            if (runtimeNativeButton.tabIndex === -1) {{
                runtimeNativeButton.removeAttribute(
                    'tabindex');
            }}
        }}
    }}

    function moveButton() {{
        attemptCount++;

        var silverInput =
            document.getElementById(
                silverClientID);

        var unitCostInput =
            document.getElementById(
                unitCostClientID);

        var innerButton =
            document.getElementById(
                buttonClientID);

        if (!silverInput ||
            !unitCostInput ||
            !innerButton) {{
            /*
             * The primary toolbar may already exist even if the runtime button
             * or one of the target controls has not finished rendering.
             */
            hideToolbarVendorQuoteButton();

            return false;
        }}

        var silverEditor =
            closestElement(
                silverInput,
                'qp-number-editor');

        var silverContainer =
            closestElement(
                silverInput,
                '.fld-c');

        var unitCostEditor =
            closestElement(
                unitCostInput,
                'qp-number-editor');

        var unitCostContainer =
            closestElement(
                unitCostInput,
                '.fld-c');

        /*
         * The ClientID belongs to the inner native button.
         *
         * Move or hide the outer qp-button so Acumatica's component remains
         * intact.
         */
        var buttonHost =
            closestElement(
                innerButton,
                'qp-button') ||
            innerButton;

        if (!silverEditor ||
            !silverContainer ||
            !unitCostEditor ||
            !unitCostContainer ||
            !buttonHost) {{
            hideToolbarVendorQuoteButton();

            return false;
        }}

        /*
         * The runtime button has now been positively identified.
         */
        identifiedRuntimeButtonHost =
            buttonHost;

        /*
         * Capture the action state before hiding the toolbar representation.
         *
         * Hiding the toolbar does not normally change its enabled state, but
         * reading and applying the state first makes the sequence explicit.
         */
        applyRuntimeButtonEnabledState(
            buttonHost,
            innerButton);

        /*
         * Hide only the primary toolbar element after positively identifying
         * the injected runtime button.
         */
        hideToolbarVendorQuoteButton();

        /*
         * The Silver control can exist in the HTML for every item even though
         * Acumatica hides it for non-Silver items.
         *
         * Hide the runtime Calc button whenever the actual Silver editor is not
         * visible.
         *
         * This visibility check must occur before either field container is
         * changed to display:flex. Otherwise the script could accidentally
         * reveal a container that Acumatica intentionally hid.
         */
        var silverFieldIsVisible =
            elementIsVisible(silverInput) &&
            elementIsVisible(silverEditor) &&
            elementIsVisible(silverContainer);

        if (!silverFieldIsVisible) {{
            buttonHost.style.setProperty(
                'display',
                'none',
                'important');

            buttonHost.setAttribute(
                'aria-hidden',
                'true');

            buttonHost.setAttribute(
                'data-istar-vendor-quote-button-applicable',
                'false');

            buttonHost.removeAttribute(
                'data-istar-vendor-quote-button-positioned');

            hideToolbarVendorQuoteButton();

            return true;
        }}

        /*
         * Unit Cost must also be visible before positioning the runtime button.
         *
         * This avoids changing a hidden field container to display:flex.
         */
        var unitCostFieldIsVisible =
            elementIsVisible(unitCostInput) &&
            elementIsVisible(unitCostEditor) &&
            elementIsVisible(unitCostContainer);

        if (!unitCostFieldIsVisible) {{
            buttonHost.style.setProperty(
                'display',
                'none',
                'important');

            buttonHost.setAttribute(
                'aria-hidden',
                'true');

            buttonHost.setAttribute(
                'data-istar-vendor-quote-button-applicable',
                'false');

            buttonHost.removeAttribute(
                'data-istar-vendor-quote-button-positioned');

            hideToolbarVendorQuoteButton();

            return true;
        }}

        /*
         * The Silver field is visible, so the runtime Calc button is
         * applicable.
         */
        buttonHost.removeAttribute(
            'aria-hidden');

        buttonHost.setAttribute(
            'data-istar-vendor-quote-button-applicable',
            'true');

        /*
         * Remove a prior display:none !important that may have been applied
         * when a non-Silver item was previously selected.
         */
        buttonHost.style.removeProperty(
            'display');

        /*
         * Place the Unit Cost editor and runtime Calc button on the same
         * horizontal row.
         */
        unitCostContainer.style.display =
            'flex';

        unitCostContainer.style.alignItems =
            'center';

        unitCostContainer.style.flexWrap =
            'nowrap';

        unitCostEditor.style.flex =
            '0 1 auto';

        buttonHost.style.setProperty(
            'display',
            'inline-block',
            'important');

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
         * Move the rendered qp-button immediately after the Unit Cost editor.
         */
        if (buttonHost.parentElement !== unitCostContainer ||
            unitCostEditor.nextElementSibling !== buttonHost) {{
            if (unitCostEditor.nextSibling) {{
                unitCostContainer.insertBefore(
                    buttonHost,
                    unitCostEditor.nextSibling);
            }}
            else {{
                unitCostContainer.appendChild(
                    buttonHost);
            }}
        }}

        buttonHost.setAttribute(
            'data-istar-vendor-quote-button-positioned',
            'true');

        /*
         * Reapply the enabled state after moving the component.
         *
         * Acumatica may update the toolbar action state while the page is
         * completing client-side initialization.
         */
        applyRuntimeButtonEnabledState(
            buttonHost,
            innerButton);

        /*
         * Acumatica may rerender the toolbar while the page is finishing its
         * client-side initialization.
         */
        hideToolbarVendorQuoteButton();

        return true;
    }}

    function attemptMove() {{
        var moveSucceeded =
            moveButton();

        /*
         * Continue retrying for approximately four seconds even after the
         * runtime button is successfully positioned.
         *
         * Acumatica may render or rerender the toolbar or action state after
         * the runtime button has already been moved.
         */
        if (attemptCount < maximumAttempts) {{
            window.setTimeout(
                attemptMove,
                retryDelayMilliseconds);

            return;
        }}

        /*
         * Perform one final position, enabled-state, and toolbar-hide attempt
         * after the retry period has completed.
         */
        moveButton();

        hideToolbarVendorQuoteButton();

        if (!moveSucceeded &&
            window.console &&
            typeof window.console.warn === 'function') {{
            window.console.warn(
                '[VendorQuoteButton] Unable to position, enable, disable, ' +
                'or hide the runtime Calc button after ' +
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
                $"{TracePrefix} Registered runtime Calc button relocation, " +
                "Silver-field applicability, Unit Cost positioning, " +
                "PXAction enabled-state mirroring, and primary-toolbar " +
                "hiding script. " +
                $"SilverClientID={rawSilverClientID}; " +
                $"UnitCostClientID={rawUnitCostClientID}; " +
                $"ButtonClientID={rawButtonClientID}; " +
                $"ToolbarIDSuffix={ToolbarElementIDSuffix}");
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