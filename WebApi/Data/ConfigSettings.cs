using System.ComponentModel.DataAnnotations;

namespace Deliver.WebApi.Data;

public class ConfigSettings
{
	public const string SectionName = "Config";

	public string ActiveDirectoryPath { get; init; } = null!;

	public string ActiveDirectoryDomain { get; init; } = null!;

	public string BypassUserName { get; init; } = null!;

	public string BypassUserPassword { get; init; } = null!;

	public List<int> SpecialHierarchies { get; init; } = new();

	public bool usesSpecialHieararhies { get; set; }

	public bool UsesCustomer { get; init; }

	public short DefaultInterval { get; init; }

	public string TableauLink { get; init; } = null!;

	public int hierarchyGlobal { get; set; }

	public int timeoutInactivity { get; set; }

	public string sQLJobSSIS { get; set; } = null!;
}
