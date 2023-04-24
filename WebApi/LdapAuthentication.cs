using System.Text;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using Deliver.WebApi.Data;

namespace Deliver.WebApi;

[SupportedOSPlatform("windows")]
public class LdapAuthentication
{
	private string _path;
	private readonly string _domain;
	public string _filterAttribute;

	public LdapAuthentication(ConfigSettings config) {
		_path = config.ActiveDirectoryPath;
		_domain = config.ActiveDirectoryDomain;
		_filterAttribute = string.Empty;
	}

	public string IsAuthenticated(string username, string pwd) {
		string sReturn = "Error authenticating user.";
		string domainAndUsername = _domain + @"\" + username;
		var entry = new DirectoryEntry(_path, domainAndUsername, pwd);

		try {
			//Bind to the native AdsObject to force authentication.
			object obj = entry.NativeObject;

			var search = new DirectorySearcher(entry) {
				Filter = $"(SAMAccountName={username})"
			};
			search.PropertiesToLoad.Add("cn");
			SearchResult? result = search.FindOne();
			if (result is null) {
				return sReturn;
			}

			//Update the new path to the user in the directory.
			_path = result.Path;
			_filterAttribute = (string)result.Properties["cn"][0];

			// Success
			return string.Empty;
		}
		catch (Exception ex) {
			//throw new Exception("Error authenticating user. " + ex.Message);
			return sReturn + " " + ex.Message;
		}
	}

	public string IsAuthenticated2(string userName, string pwd) {
		string sReturn = "Active Directory domain authentication failed.";
		try {
			using (var context = new PrincipalContext(ContextType.Domain, _path, userName, pwd)) {
				var options = ContextOptions.Negotiate | ContextOptions.Signing | ContextOptions.Sealing;
				if (!context.ValidateCredentials(userName, pwd, options)) {
					return sReturn;
				}
			}

			return string.Empty;  // Success
		}
		catch (Exception ex) {
			//throw new Exception("Error authenticating user. " + ex.Message);
			return $"{sReturn} {ex.Message}";
		}
	}

	public string? GetGroups() {
		var search = new DirectorySearcher(_path) {
			Filter = "(cn=" + _filterAttribute + ")"
		};
		search.PropertiesToLoad.Add("memberOf");
		var groupNames = new StringBuilder();

		try {
			SearchResult? searchResult = search.FindOne();
			int propertyCount = searchResult!.Properties["memberOf"].Count;
			string dn;
			int equalsIndex, commaIndex;

			for (int propertyCounter = 0; propertyCounter < propertyCount; propertyCounter++) {
				dn = (string)searchResult.Properties["memberOf"][propertyCounter];
				equalsIndex = dn.IndexOf("=", 1);
				commaIndex = dn.IndexOf(",", 1);
				if (-1 == equalsIndex) {
					return null;
				}

				groupNames.Append(dn.AsSpan(equalsIndex + 1, commaIndex - equalsIndex - 1));
				groupNames.Append('|');
			}
		}
		catch (Exception ex) {
			throw new Exception("Error obtaining group names. " + ex.Message);
		}

		return groupNames.ToString();
	}
}
