using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace CLS.WebApi.Data;

public class ConfigurationObject
{
	public const string Section = "Config";

	public string activeDiretoryPath { get; set; } = null!;

	public string activeDiretoryDomain { get; set; } = null!;

	public string byPassUserName { get; set; } = null!;

	public string byPassUserPassword { get; set; } = null!;

	public List<int> specialHierarhies { get; set; } = new();

	public bool usesSpecialHieararhies { get; set; }

	public bool usesCustomer { get; set; }

	public short DefaultInterval { get; set; }

	public string tableauLink { get; set; } = null!;

	public int hierarchyGlobal { get; set; }

	public int timeoutInactivity { get; set; }

	public string sQLJobSSIS { get; set; } = null!;
}
