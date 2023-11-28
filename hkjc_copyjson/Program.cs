using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Commerce.Plugin.Views;
using System.Configuration;
using System.Data.SqlClient;


string connectionString = ConfigurationManager.AppSettings["connectionString"];
string dbandTableName = ConfigurationManager.AppSettings["dbandTableName"];
//string connectionString = "Data Source=127.0.0.1;DataBase=SitecoreCommerce_SharedEnvironments;User ID=sa;Password=123456";
using (SqlConnection connection = new SqlConnection(connectionString))
{
	try
	{
		List<CustomersEntity> customersEntities = new List<CustomersEntity>();
		connection.Open();
		//string sqlQuery = "SELECT UniqueId,ArtifactStoreId,Entity FROM SitecoreCommerce_SharedEnvironments.dbo.test2";
		string sqlQuery = "SELECT UniqueId,ArtifactStoreId,Entity FROM "+ dbandTableName;
		SqlCommand com = new SqlCommand(sqlQuery, connection);
		SqlDataReader reader = com.ExecuteReader();
		if (reader != null)
		{
			while (reader.Read())
			{
				var customersEntity = new CustomersEntity();
				customersEntity.UniqueId = reader["UniqueId"].ToString();
				customersEntity.ArtifactStoreId = reader["ArtifactStoreId"].ToString();
				customersEntity.Entity = reader["Entity"].ToString();
				customersEntities.Add(customersEntity);
			}
			Console.WriteLine("customersEntities.Count:" + customersEntities.Count());
			reader.Close();
			reader.Dispose();
		}
		int i = 1;
		foreach (var item in customersEntities.Where(x => x.Entity.StartsWith("{\"$type\":\"Sitecore.Commerce.Plugin.Customers.Customer, Sitecore.Commerce.Plugin.Customers\",")))
		{
			Console.WriteLine(i++ + ":" + item.UniqueId);
			var json = UpgradeJson(item.Entity);
			if (json != item.Entity)
			{
				//string sqlCommand = "update SitecoreCommerce_SharedEnvironments.dbo.test2 set Entity = '" + json + "' where UniqueId = '" + item.UniqueId + "'";
				string sqlCommand = "update "+ dbandTableName+" set Entity = '" + json + "' where UniqueId = '" + item.UniqueId + "'";

				SqlCommand command = new SqlCommand(sqlCommand, connection);
				int rows = command.ExecuteNonQuery();
				if (rows > 0) { Console.WriteLine("update succeed"); } else { Console.WriteLine("update failed"); }
			}
			else
			{
				Console.WriteLine("not need to update");
			}
			Console.WriteLine("----------------------------------");
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine("error:" + ex);
	}
	finally { connection.Close(); }

}

string UpgradeJson(string json)
{
	//var obj = JsonConvert.DeserializeObject<Customer>(json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
	var obj = CommerceEntity.Inflate<Customer>(json);
	if (obj != null && obj.ToString() == "Sitecore.Commerce.Plugin.Customers.Customer")
	{
		//找第一个
		var customerDetailsComponent = obj.EntityComponents.FirstOrDefault(x => x.ToString() == "Sitecore.Commerce.Plugin.Customers.CustomerDetailsComponent");
		if (customerDetailsComponent != null)
		{
			//找第一个中符合的部分
			var views = ((EntityViewComponent)customerDetailsComponent).View.ChildViews.Where(x => ((EntityView)x).ItemId.StartsWith("Composer-"));
			if (views.Count() > 0)
			{
				//找新增的那个
				var entityViewComponent = obj.EntityComponents.FirstOrDefault(x => x.ToString() == "Sitecore.Commerce.Plugin.Views.EntityViewComponent");
				if (entityViewComponent != null)
				{
					var viewsItemIds = ((EntityViewComponent)entityViewComponent).View.ChildViews.Where(x => ((EntityView)x).ItemId.StartsWith("Composer-")).Select(x => ((EntityView)x).ItemId).ToList();
					foreach (var item in views)
					{
						if (!viewsItemIds.Contains(((EntityView)item).ItemId))
						{
							((EntityViewComponent)entityViewComponent).View.ChildViews.Add(item);
						}
					}
				}
				else
				{
					entityViewComponent = new EntityViewComponent()
					{
						Id = Guid.NewGuid().ToString().Replace("-", "").Replace("{", "").Replace("}", "").ToLower()
					};
					foreach (var item in views)
					{
						((EntityViewComponent)entityViewComponent).View.ChildViews.Add(item);
					}

					obj.AddComponents(entityViewComponent);
				}

			}
		}

		//var j=JsonConvert.SerializeObject(obj, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto ,DefaultValueHandling=DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore });
		return CommerceEntity.Deflate(obj);
	}
	else
	{
		return json;
	}
}



public class CustomersEntity
{
	public string UniqueId { get; set; } = string.Empty;
	public string ArtifactStoreId { get; set; } = string.Empty;
	public string Entity { get; set; } = string.Empty;
}