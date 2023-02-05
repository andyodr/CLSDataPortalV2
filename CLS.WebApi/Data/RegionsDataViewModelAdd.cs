namespace CLS.WebApi.Data;

public class RegionsDataViewModelAdd
{
	public int? id { get; set; }

	public int levelId { get; set; }

	public string level { get; set; }

	public string name { get; set; }

	public int? parentId { get; set; }

	public string parentName { get; set; }

	public bool active { get; set; }

	public bool remove { get; set; }
}
