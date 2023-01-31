namespace CLS.WebApi;

public class Resource
{
	public const string USER_NOT_AUTHORIZED = "You are not authorized to view this page.";
	public const string USER_AUTHORIZATION_ERR = "Error authenticating user.";
	public const string PAGE_AUTHORIZATION_ERR = "ERR";
	public const string SQL_JOB_ERR = @"SQL Job ""{0}"" failed.";


	public const string MEASURE_DATA = "Measure Data";
	public const string TARGET = "Target";
	public const string HIERARCHY = "Hierarchy";
	public const string MEASURE_DEFINITION = "Measure Definition";
	public const string MEASURE_TYPE = "Measure Type";
	public const string MEASURE = "Measure";
	public const string DATA_IMPORT = "Data Import";
	public const string USERS = "Users";
	public const string SETTINGS = "Settings";
	public const string SETTINGS_USERS = "Settings Users";
	public const string SECURITY = "Security";
	public const string SYSTEM = "System";
	public const string WEB_PAGES = "Web Pages";
	public const string WEB_SITE = "Web Site";
	public const string FILTERS = "Filters";

	public const string ERR_STRING_TO_BYTE = "String value doesn't represent a byte value.";
	public const string ERR_STRING_TO_BOOL = "String value doesn't represent a boolean value.";
	public const string ERR_BYTE_TO_STRING = "Byte value doesn't represent a boolean value.";
	public const string ERR_STRING_TO_STRING = "Boolean value doesn't represent a string value.";
	public const string ERR_INTERVAL_ID = "Measure Definition Record id: {0} does not have any intervals selected.";

	// Validation
	public const string VAL_VALID_INTERVAL_ID = "Please enter a valid interval ID.";
	public const string VAL_INVALID_INTERVAL_ID = "Invalid interval ID.";
	public const string VAL_VALUE_UNIT = "Value must be between 0 and 1 since the unit is %.";
	public const string VAL_TARGET_UNIT = "Target must be between 0 and 1 since the unit is %.";
	public const string VAL_YELLOW_UNIT = "Yellow must be between 0 and 1 since the unit is %.";
	public const string VAL_MEASURE_TYPE_EXIST = "Measure Type already exists.";
	public const string VAL_MEASURE_DEF_NAME_EXIST = "The Measure Definition Name or Variable Name already exists.";
	public const string VAL_USERNAME_PASSWORD = "Username and Password cannot be empty.";
	public const string VAL_USERNAME_NOT_FOUND = "Username cannot be found.";

	// Data Import
	public const string DI_FILTER_INVALID_INTERVAL = "Invalid Interval ID Passed to filter get method for Data Import.";
	public const string DI_NO_MONTH = "There is no month for the current date.";
	public const string DI_ERR_UPLOADING = "ERROR UPLOADING THIS ROW";
	public const string DI_ERR_USER_DATE = "This user cannot input data for this date.";
	public const string DI_ERR_TARGET_NO_EXIST = "Target does not exist.";
	public const string DI_ERR_HIEARCHY_NO_EXIST = "Hierarchy does not exist.";
	public const string DI_ERR_HIEARCHY_VALUE = "A Value for this Hierarchy cannot be entered.";
	public const string DI_ERR_HIEARCHY_NO_ACTIVE = "Hierarchy is not Active. Please contact the Administrator.";
	public const string DI_ERR_HIEARCHY_NO_ACCESS = "User does not have access to this Hierarchy.";
	public const string DI_ERR_MEASURE_NULL = "MeasureID cannot be NULL.";
	public const string DI_ERR_HIEARCHY_NULL = "HierarchyID cannot be NULL.";
	public const string DI_ERR_TARGET_NULL = "Target cannot be NULL.";
	public const string DI_ERR_CALENDAR_NULL = "CalendarId cannot be NULL.";
	public const string DI_ERR_CALENDAR_NO_EXIST = "CalendarId does not exist.";
	public const string DI_ERR_CALCULATED = "There cannot be a value for this row since this record is calculated.";
	public const string DI_ERR_NO_MEASURE = "There is no Measure Record for these ID's.";
	public const string DI_ERR_NO_MEASURE_DATA = "There is no MeasureData Record. Please contact Administrator.";
	public const string DI_ERR_TARGET_REPEATED = "There are repeated Targets with the same Hierarchy and Measure.";

	// Hierarchy
	public const string HIERARCHY_ERR_DELETE = "This Hierarchy is in use in other tables and cannot be deleted.";

	// Settings
	public const string SETTINGS_NO_RECORDS = "Setting table does not have any records.";

	// Users
	public const string USERS_EXIST = "This UserName already exist.";
}
