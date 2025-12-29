// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.ApplicationModel.Resources;

namespace BitwardenForCommandPalette.Helpers;

/// <summary>
/// Helper class for accessing string resources
/// </summary>
internal static class ResourceHelper
{
    private static readonly ResourceLoader _resourceLoader = new("Resources");

    /// <summary>
    /// Gets a localized string resource by key
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <returns>The localized string, or the key if not found</returns>
    public static string GetString(string key)
    {
        try
        {
            var value = _resourceLoader.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch
        {
            return key;
        }
    }

    /// <summary>
    /// Gets a formatted localized string resource
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>The formatted localized string</returns>
    public static string GetString(string key, params object[] args)
    {
        try
        {
            var format = GetString(key);
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, format, args);
        }
        catch
        {
            return key;
        }
    }

    // Application
    public static string AppDisplayName => GetString("AppDisplayName");

    // Common Actions
    public static string ActionOpen => GetString("ActionOpen");
    public static string ActionUnlock => GetString("ActionUnlock");
    public static string ActionFilter => GetString("ActionFilter");
    public static string ActionApplyFilter => GetString("ActionApplyFilter");
    public static string ActionRefresh => GetString("ActionRefresh");
    public static string ActionNoAction => GetString("ActionNoAction");

    // Copy Commands
    public static string CommandCopyPassword => GetString("CommandCopyPassword");
    public static string CommandCopyUsername => GetString("CommandCopyUsername");
    public static string CommandCopyUrl => GetString("CommandCopyUrl");
    public static string CommandOpenUrl => GetString("CommandOpenUrl");
    public static string CommandCopyTotp => GetString("CommandCopyTotp");
    public static string CommandCopyCardNumber => GetString("CommandCopyCardNumber");
    public static string CommandCopyCvv => GetString("CommandCopyCvv");
    public static string CommandCopyExpiration => GetString("CommandCopyExpiration");
    public static string CommandCopyCardholderName => GetString("CommandCopyCardholderName");
    public static string CommandCopyFullName => GetString("CommandCopyFullName");
    public static string CommandCopyEmail => GetString("CommandCopyEmail");
    public static string CommandCopyPhone => GetString("CommandCopyPhone");
    public static string CommandCopyAddress => GetString("CommandCopyAddress");
    public static string CommandCopyCompany => GetString("CommandCopyCompany");
    public static string CommandCopySsn => GetString("CommandCopySsn");
    public static string CommandCopyPassportNumber => GetString("CommandCopyPassportNumber");
    public static string CommandCopyLicenseNumber => GetString("CommandCopyLicenseNumber");
    public static string CommandCopyNotes => GetString("CommandCopyNotes");
    public static string CommandSyncVault => GetString("CommandSyncVault");
    public static string CommandLockVault => GetString("CommandLockVault");

    // Toast Messages
    public static string ToastTotpCopied(string totp) => GetString("ToastTotpCopied", totp);
    public static string ToastTotpFailed => GetString("ToastTotpFailed");
    public static string ToastCardNumberCopied(string lastFour) => GetString("ToastCardNumberCopied", lastFour);
    public static string ToastCvvCopied => GetString("ToastCvvCopied");
    public static string ToastExpirationCopied(string expiration) => GetString("ToastExpirationCopied", expiration);
    public static string ToastCardholderNameCopied => GetString("ToastCardholderNameCopied");
    public static string ToastNameCopied => GetString("ToastNameCopied");
    public static string ToastEmailCopied => GetString("ToastEmailCopied");
    public static string ToastPhoneCopied => GetString("ToastPhoneCopied");
    public static string ToastAddressCopied => GetString("ToastAddressCopied");
    public static string ToastCompanyCopied => GetString("ToastCompanyCopied");
    public static string ToastSsnCopied => GetString("ToastSsnCopied");
    public static string ToastPassportCopied => GetString("ToastPassportCopied");
    public static string ToastLicenseCopied => GetString("ToastLicenseCopied");
    public static string ToastNotesCopied => GetString("ToastNotesCopied");
    public static string ToastFieldCopied(string fieldName) => GetString("ToastFieldCopied", fieldName);
    public static string ToastVaultSynced => GetString("ToastVaultSynced");
    public static string ToastVaultSyncFailed => GetString("ToastVaultSyncFailed");
    public static string ToastVaultLocked => GetString("ToastVaultLocked");
    public static string ToastVaultLockFailed => GetString("ToastVaultLockFailed");
    public static string ToastUsernameCopied => GetString("ToastUsernameCopied");

    // Unlock Page
    public static string UnlockPageTitle => GetString("UnlockPageTitle");
    public static string UnlockCardTitle => GetString("UnlockCardTitle");
    public static string UnlockCardDescription => GetString("UnlockCardDescription");
    public static string UnlockMasterPasswordLabel => GetString("UnlockMasterPasswordLabel");
    public static string UnlockMasterPasswordPlaceholder => GetString("UnlockMasterPasswordPlaceholder");
    public static string UnlockMasterPasswordRequired => GetString("UnlockMasterPasswordRequired");
    public static string UnlockButtonText => GetString("UnlockButtonText");
    public static string UnlockSuccess => GetString("UnlockSuccess");
    public static string UnlockFailed => GetString("UnlockFailed");

    // Filter Page
    public static string FilterPageTitle => GetString("FilterPageTitle");
    public static string FilterPagePlaceholder => GetString("FilterPagePlaceholder");
    public static string FilterLoadingFolders => GetString("FilterLoadingFolders");
    public static string FilterAllItems => GetString("FilterAllItems");
    public static string FilterAllItemsSubtitle => GetString("FilterAllItemsSubtitle");
    public static string FilterFavoritesOnly => GetString("FilterFavoritesOnly");
    public static string FilterFavoritesSubtitle => GetString("FilterFavoritesSubtitle");
    public static string FilterLoginsOnly => GetString("FilterLoginsOnly");
    public static string FilterLoginsSubtitle => GetString("FilterLoginsSubtitle");
    public static string FilterCardsOnly => GetString("FilterCardsOnly");
    public static string FilterCardsSubtitle => GetString("FilterCardsSubtitle");
    public static string FilterIdentitiesOnly => GetString("FilterIdentitiesOnly");
    public static string FilterIdentitiesSubtitle => GetString("FilterIdentitiesSubtitle");
    public static string FilterNotesOnly => GetString("FilterNotesOnly");
    public static string FilterNotesSubtitle => GetString("FilterNotesSubtitle");
    public static string FilterByFolder => GetString("FilterByFolder");
    public static string FilterNoFolder => GetString("FilterNoFolder");
    public static string FilterNoFolderSubtitle => GetString("FilterNoFolderSubtitle");
    public static string FilterFolderItem(string folderName) => GetString("FilterFolderItem", folderName);
    public static string FilterFolderSubtitle => GetString("FilterFolderSubtitle");
    public static string FilterTagActive => GetString("FilterTagActive");

    // Main Page
    public static string MainPageTitle => GetString("MainPageTitle");
    public static string MainPagePlaceholder => GetString("MainPagePlaceholder");
    public static string MainFilterButton => GetString("MainFilterButton");
    public static string MainSyncButton => GetString("MainSyncButton");
    public static string MainSyncSubtitle => GetString("MainSyncSubtitle");
    public static string MainLockButton => GetString("MainLockButton");
    public static string MainLockSubtitle => GetString("MainLockSubtitle");
    public static string MainUnlockButton => GetString("MainUnlockButton");
    public static string MainUnlockSubtitle => GetString("MainUnlockSubtitle");

    // Status Messages
    public static string StatusLoading => GetString("StatusLoading");
    public static string StatusLoadingSubtitle => GetString("StatusLoadingSubtitle");
    public static string StatusError => GetString("StatusError");
    public static string StatusNotLoggedIn => GetString("StatusNotLoggedIn");
    public static string StatusNotLoggedInSubtitle => GetString("StatusNotLoggedInSubtitle");
    public static string StatusNoItems => GetString("StatusNoItems");
    public static string StatusNoItemsSubtitle => GetString("StatusNoItemsSubtitle");
    public static string StatusCliNotInstalled => GetString("StatusCliNotInstalled");
    public static string StatusLoadItemsFailed(string error) => GetString("StatusLoadItemsFailed", error);

    // Filter Descriptions
    public static string FilterDescFavorites => GetString("FilterDescFavorites");
    public static string FilterDescLogins => GetString("FilterDescLogins");
    public static string FilterDescCards => GetString("FilterDescCards");
    public static string FilterDescIdentities => GetString("FilterDescIdentities");
    public static string FilterDescNotes => GetString("FilterDescNotes");
    public static string FilterDescAllTypes => GetString("FilterDescAllTypes");
    public static string FilterDescFolder(string folderName) => GetString("FilterDescFolder", folderName);
    public static string FilterDescNoFilter => GetString("FilterDescNoFilter");

    // Item Subtitles
    public static string ItemSubtitleSecureNote => GetString("ItemSubtitleSecureNote");
    public static string ItemSubtitleUnnamed => GetString("ItemSubtitleUnnamed");
    public static string ItemTagFavorite => GetString("ItemTagFavorite");

    // Settings
    public static string SettingsBwPathLabel => GetString("SettingsBwPathLabel");
    public static string SettingsBwPathDescription => GetString("SettingsBwPathDescription");
    public static string SettingsBwClientIdLabel => GetString("SettingsBwClientIdLabel");
    public static string SettingsBwClientIdDescription => GetString("SettingsBwClientIdDescription");
    public static string SettingsBwClientSecretLabel => GetString("SettingsBwClientSecretLabel");
    public static string SettingsBwClientSecretDescription => GetString("SettingsBwClientSecretDescription");
    public static string SettingsCustomEnvLabel => GetString("SettingsCustomEnvLabel");
    public static string SettingsCustomEnvDescription => GetString("SettingsCustomEnvDescription");

    // Details Panel - Common
    public static string DetailsUsername => GetString("DetailsUsername");
    public static string DetailsPassword => GetString("DetailsPassword");
    public static string DetailsTotp => GetString("DetailsTotp");
    public static string DetailsEnabled => GetString("DetailsEnabled");
    public static string DetailsUrls => GetString("DetailsUrls");
    public static string DetailsWebsite => GetString("DetailsWebsite");
    public static string DetailsNotes => GetString("DetailsNotes");
    public static string DetailsCustomFields => GetString("DetailsCustomFields");

    // Details Panel - Card
    public static string DetailsBrand => GetString("DetailsBrand");
    public static string DetailsCardNumber => GetString("DetailsCardNumber");
    public static string DetailsExpiration => GetString("DetailsExpiration");
    public static string DetailsCvv => GetString("DetailsCvv");
    public static string DetailsCardholderName => GetString("DetailsCardholderName");

    // Details Panel - Identity
    public static string DetailsPersonalInfo => GetString("DetailsPersonalInfo");
    public static string DetailsFullName => GetString("DetailsFullName");
    public static string DetailsEmail => GetString("DetailsEmail");
    public static string DetailsPhone => GetString("DetailsPhone");
    public static string DetailsCompany => GetString("DetailsCompany");
    public static string DetailsAddress => GetString("DetailsAddress");
    public static string DetailsIdentification => GetString("DetailsIdentification");
    public static string DetailsSsn => GetString("DetailsSsn");
    public static string DetailsPassport => GetString("DetailsPassport");
    public static string DetailsLicense => GetString("DetailsLicense");
}
