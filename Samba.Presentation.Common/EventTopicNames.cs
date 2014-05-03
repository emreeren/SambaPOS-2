﻿using System;

namespace Samba.Presentation.Common
{
    public static class RuleEventNames
    {
        public const string TicketLineCancelled = "TicketLineCancelled";
        public const string ModifierSelected = "ModifierSelected";
        public const string PortionSelected = "PortionSelected";
        public const string ApplicationStarted = "ApplicationStarted";
        public const string TicketClosed = "TicketClosed";
        public const string ChangeAmountChanged = "ChangeAmountChanged";
        public const string TicketLineAdded = "TicketLineAdded";
        public const string PaymentReceived = "PaymentReceived";
        public const string TicketLocationChanged = "TicketLocationChanged";
        public const string TriggerExecuted = "TriggerExecuted";
        public const string TicketTotalChanged = "TicketTotalChanged";
        public const string TicketTagSelected = "TicketTagSelected";
        public const string CustomerSelectedForTicket = "CustomerSelectedForTicket";
        public const string TicketCreated = "TicketCreated";
        public const string WorkPeriodStarts = "WorkPeriodStarts";
        public const string WorkPeriodEnds = "WorkPeriodEnds";
        public const string UserLoggedOut = "UserLoggedOut";
        public const string UserLoggedIn = "UserLoggedIn";
        public const string MessageReceived = "MessageReceived";
        public const string CashDrawerManuallyOpened = "CashDrawerManuallyOpened";
        public const string OnExceptionOccured = "OnExceptionOccured";
    }

    public static class EventTopicNames
    {
        public const string PaymentProcessed = "Payment Processed";
        public const string FocusTicketScreen = "FocusTicketScreen";
        public const string AddLiabilityAmount = "Add Liability Amount";
        public const string AddReceivableAmount = "Add Receivable Amount";
        public const string LocationSelectedForTicket = "LocationSelectedForTicket";
        public const string ExecuteEvent = "ExecuteEvent";
        public const string UpdateDepartment = "Update Department";
        public const string PopupClicked = "Popup Clicked";
        public const string DisplayTicketExplorer = "Display Ticket Explorer";
        public const string TagSelectedForSelectedTicket = "Tag Selected";
        public const string SelectTicketTag = "Select Ticket Tag";
        public const string LogData = "Log Data";
        public const string ResetNumerator = "Reset Numerator";
        public const string WorkPeriodStatusChanged = "WorkPeriod Status Changed";
        public const string BrowseUrl = "Browse Url";
        public const string ActivateCustomerAccount = "Activate Customer Account";
        public const string ActivateCustomerView = "Activate Customer View";
        public const string SelectExtraProperty = "Select Extra Property";
        public const string SelectVoidReason = "Select Void Reason";
        public const string SelectGiftReason = "Select Gift Reason";
        public const string AddExtraModifiers = "Add Extra Modifiers";
        public const string ActivateNavigation = "Activate Navigation";
        public const string CustomerSelectedForTicket = "Customer Selected For Ticket";
        public const string SelectCustomer = "Select Customer";
        public const string NavigationCommandAdded = "Navigation Command Added";
        public const string DashboardCommandAdded = "Dashboard Command Added";
        public const string SelectedTicketChanged = "Selected Ticket Changed";
        public const string TicketItemAdded = "Ticket Item Added";
        public const string DashboardClosed = "Dashboard Closed";
        public const string MessageReceivedEvent = "Message Received";
        public const string ViewAdded = "View Added";
        public const string ViewClosed = "View Closed";
        public const string PinSubmitted = "Pin Submitted";
        public const string UserLoggedIn = "User LoggedIn";
        public const string UserLoggedOut = "User LoggedOut";
        public const string AddedModelSaved = "ModelSaved";
        public const string ModelAddedOrDeleted = "Model Added or Deleted";
        public const string MakePayment = "Make Payment";
        public const string PaymentSubmitted = "Payment Submitted";
        public const string SelectedItemsChanged = "Selected Items Changed";
        public const string SelectedDepartmentChanged = "Selected Department Changed";
        public const string SelectTable = "Select Table";
        public const string FindTable = "Find Table";
        public const string ActivateTicketView = "Activate Ticket View";
        public const string DisplayTicketView = "Display Ticket View";
        public const string RefreshSelectedTicket = "Refresh Selected Ticket";
        public const string EditTicketNote = "Edit Ticket Note";
        public const string PaymentRequestedForTicket = "Payment Requested For Ticket";
        public const string GetPaymentFromCustomer = "Get Payment From Customer";
        public const string MakePaymentToCustomer = "Make Payment To Customer";
    }
}
