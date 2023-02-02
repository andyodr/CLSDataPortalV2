using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace CLS.WebApi.Data;

public class ConfigurationObject
{
	public const string Section = "Config";

	public string connectionString { get; set; }

	public string activeDiretoryPath { get; set; }

	public string activeDiretoryDomain { get; set; }

	public string byPassUserName { get; set; }

	public string byPassUserPassword { get; set; }

	public List<int> specialHierarhies { get; set; }

	public bool usesSpecialHieararhies { get; set; }

	public bool usesCustomer { get; set; }

	public string tableauLink { get; set; }

	public int hierarchyGlobal { get; set; }

	public int timeoutInactivity { get; set; }

	public string sQLJobSSIS { get; set; }
}
