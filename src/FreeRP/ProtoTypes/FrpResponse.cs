using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace FreeRP
{
    public partial class FrpResponse
    {
        public static FrpResponse ErrorNone() => new() { ErrorType = FrpErrorType.ErrorNone };
        public static FrpResponse Create(string data) => new() { ErrorType = FrpErrorType.ErrorNone, Data = data };
        public static FrpResponse Create(IMessage data) => new() { ErrorType = FrpErrorType.ErrorNone, AnyData = Any.Pack(data) };
        public static FrpResponse ErrorUnknown(string msg) => new() { ErrorType = FrpErrorType.ErrorUnknown, Message = msg };
        public static FrpResponse CreateBool(bool val) => new() { BoolValue = val };

        public static FrpResponse Create(FrpErrorType errorType, Localization.FrpLocalizationService i18n)
        {
            FrpResponse res = new() { 
                ErrorType = errorType,
            };

            switch (errorType)
            {
                case FrpErrorType.ErrorUnknown:
                    res.Message = i18n.Text.ErrorUnknown;
                    break;
                case FrpErrorType.ErrorAccessDenied:
                    res.Message = i18n.Text.ErrorAccessDenied;
                    break;
                case FrpErrorType.ErrorEmailInvalid:
                    res.Message = i18n.Text.ErrorEmailInvalid;
                    break;
                case FrpErrorType.ErrorPathExist:
                    res.Message = i18n.Text.ErrorPathExist;
                    break;
                case FrpErrorType.ErrorPathNotExist:
                    res.Message = i18n.Text.ErrorPathNotExist;
                    break;
                case FrpErrorType.ErrorNotFound:
                    res.Message = i18n.Text.ErrorNotFound;
                    break;

                case FrpErrorType.ErrorPasswordToShort:
                    res.Message = i18n.Text.ErrorPasswordToShort;
                    break;
                case FrpErrorType.ErrorPasswordNumber:
                    res.Message = i18n.Text.ErrorPasswordNumber;
                    break;
                case FrpErrorType.ErrorPasswordUpperChar:
                    res.Message = i18n.Text.ErrorPasswordUpperChar;
                    break;
                case FrpErrorType.ErrorPasswordLowerChar:
                    res.Message = i18n.Text.ErrorPasswordLowerChar;
                    break;
                case FrpErrorType.ErrorPasswordSymbols:
                    res.Message = i18n.Text.ErrorPasswordSymbols;
                    break;

                case FrpErrorType.ErrorUserExist:
                    res.Message = i18n.Text.ErrorUserExist;
                    break;
                case FrpErrorType.ErrorUserNotExist:
                    res.Message = i18n.Text.ErrorUserNotExist;
                    break;

                case FrpErrorType.ErrorRoleExist:
                    res.Message = i18n.Text.ErrorRoleExist;
                    break;
                case FrpErrorType.ErrorRoleNotExist:
                    res.Message = i18n.Text.ErrorRoleNotExist;
                    break;
                case FrpErrorType.ErrorRoleNameRequired:
                    res.Message = i18n.Text.ErrorRoleNameRequired;
                    break;

                case FrpErrorType.ErrorProjectExist:
                    res.Message = i18n.Text.ErrorProjectExist;
                    break;
                case FrpErrorType.ErrorProjectNotExist:
                    res.Message = i18n.Text.ErrorProjectNotExist;
                    break;
                case FrpErrorType.ErrorProjectRoleExist:
                    res.Message = i18n.Text.ErrorProjectRoleExist;
                    break;
                case FrpErrorType.ErrorProjectNotRoleExist:
                    res.Message = i18n.Text.ErrorProjectNotRoleExist;
                    break;
                case FrpErrorType.ErrorProjectNameRequired:
                    res.Message = i18n.Text.ErrorProjectNameRequired;
                    break;

                case FrpErrorType.ErrorDatabaseOnlyUserAccess:
                    res.Message = i18n.Text.ErrorDatabaseOnlyUserAccess;
                    break;
                case FrpErrorType.ErrorDatabaseOnlyRoleAccess:
                    res.Message = i18n.Text.ErrorDatabaseOnlyRoleAccess;
                    break;
                case FrpErrorType.ErrorDatabaseOnlyProjectAccess:
                    res.Message = i18n.Text.ErrorDatabaseOnlyProjectAccess;
                    break;
                case FrpErrorType.ErrorDatabaseExist:
                    res.Message = i18n.Text.ErrorDatabaseExist;
                    break;
                case FrpErrorType.ErrorDatabaseNotExist:
                    res.Message = i18n.Text.ErrorDatabaseNotExist;
                    break;
                case FrpErrorType.ErrorDatabaseIdRequired:
                    res.Message = i18n.Text.ErrorDatabaseIdRequired;
                    break;
                case FrpErrorType.ErrorDatabaseIsNotOpen:
                    res.Message = i18n.Text.ErrorDatabaseIsNotOpen;
                    break;
                case FrpErrorType.ErrorDatabaseNotAllowedUnkownData:
                    res.Message = i18n.Text.ErrorDatabaseNotAllowedUnkownData;
                    break;

                case FrpErrorType.ErrorDatasetExist:
                    res.Message = i18n.Text.ErrorDataStructureExist;
                    break;
                case FrpErrorType.ErrorDatasetNotExist:
                    res.Message = i18n.Text.ErrorDataStructureNotExist;
                    break;
                case FrpErrorType.ErrorDatasetIdRequired:
                    res.Message = i18n.Text.ErrorDataStructureIdRequired;
                    break;
                case FrpErrorType.ErrorDatasetPrimaryKeyRequired:
                    res.Message = i18n.Text.ErrorDataStructurePrimaryKeyRequired;
                    break;
                case FrpErrorType.ErrorDatasetDataInvalid:
                    res.Message = i18n.Text.ErrorDataSetInvalid;
                    break;

                case FrpErrorType.ErrorFieldExist:
                    res.Message = i18n.Text.ErrorDataFieldExist;
                    break;
                case FrpErrorType.ErrorFieldNotExist:
                    res.Message = i18n.Text.ErrorDataFieldNotExist;
                    break;
                case FrpErrorType.ErrorFieldIdRequired:
                    res.Message = i18n.Text.ErrorDataFieldIdRequired;
                    break;

                case FrpErrorType.ErrorUriSchemeNotSupported:
                    res.Message = i18n.Text.ErrorUriSchemeNotSupported;
                    break;
                case FrpErrorType.ErrorUriSchemeFileRequired:
                    res.Message = i18n.Text.ErrorUriSchemeFileRequired;
                    break;
                case FrpErrorType.ErrorUriSchemeDatabaseRequired:
                    res.Message = i18n.Text.ErrorUriSchemeDatabaseRequired;
                    break;

                case FrpErrorType.ErrorFileExist:
                    res.Message = i18n.Text.ErrorFileExist;
                    break;
                case FrpErrorType.ErrorFileNotExist:
                    res.Message = i18n.Text.ErrorFileNotExist;
                    break;
                case FrpErrorType.ErrorDirectoryExist:
                    res.Message = i18n.Text.ErrorDirectoryExist;
                    break;
                case FrpErrorType.ErrorDirectoryNotExist:
                    res.Message = i18n.Text.ErrorDirectoryNotExist;
                    break;

                case FrpErrorType.ErrorAccessConflict:
                    res.Message = i18n.Text.ErrorAccessConflict;
                    break;
                case FrpErrorType.ErrorAccessExist:
                    res.Message = i18n.Text.ErrorAccessExist;
                    break;
                case FrpErrorType.ErrorAccessNotExist:
                    res.Message = i18n.Text.ErrorAccessNotExist;
                    break;
                case FrpErrorType.ErrorPluginExist:
                    break;
                case FrpErrorType.ErrorPluginNotExist:
                    break;
                default:
                    break;
            }

            return res;
        }
    }
}
