using Deliver.WebApi.Data;

namespace Deliver.WebApi.Extensions;

public static class Extensions
{
	public static IList<RegionsDataViewModel> OrderByHierarchy(this IList<RegionsDataViewModel> v, RegionsDataViewModel root = null!)
	{
		List<RegionsDataViewModel> dest = [];
		if (root is null) {
			root = v.First(h => h.ParentId is null);
			dest = [root];
		}

		var children = v.Where(h => h.ParentId == root?.Id).OrderByDescending(h => h.Active).ThenBy(h => h.Id);
		foreach (var node in children) {
			dest.Add(node);
			dest.AddRange(v.OrderByHierarchy(node));
		}

		return dest;
	}

	public static double? RoundNullable(this double? value, int digits) {
		return value switch {
			double v => Math.Round(v, digits, MidpointRounding.AwayFromZero),
			null => null
		};
	}
}
