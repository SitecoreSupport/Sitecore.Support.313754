using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using Sitecore.Shell.Applications.Dialogs.SortContent;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitecore.Support.Shell.Applications.Dialogs.Sort
{
  /// <summary>
  /// The reorder.
  /// </summary>
  public class SortForm : Sitecore.Shell.Applications.Dialogs.Sort.SortForm
  {
    /// <summary>
    /// The on ok.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The args.</param>
    /// <remarks>When the user clicks OK, the dialog is closed by calling
    /// the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.</remarks>
    protected override void OnOK(object sender, EventArgs args)
    {
      Assert.ArgumentNotNull(sender, "sender");
      Assert.ArgumentNotNull(args, "args");
      ListString listString = new ListString(WebUtil.GetFormValue("sortorder"));
      if (listString.Count == 0)
      {
        base.OnOK(sender, args);
      }
      else
      {
        Sort(from i in listString
             select ShortID.DecodeID(i));
        SheerResponse.SetDialogValue("1");
        base.OnOK(sender, args);
      }
    }

    /// <summary>
    /// Sorts the specified order.
    /// </summary>
    /// <param name="orderList">The orderList.</param>
    private void Sort(IEnumerable<ID> orderList)
    {
      Assert.ArgumentNotNull(orderList, "orderList");
      SortContentOptions sortContentOptions = SortContentOptions.Parse();
      int num = Settings.DefaultSortOrder;
      Item[] itemsToSort = GetItemsToSort(sortContentOptions.Item, sortContentOptions.ContentToSortQuery);
      foreach (ID order in orderList)
      {
        ID idToFind = order;
        Item item = Array.Find(itemsToSort, (Item i) => i.ID == idToFind);
        if (item != null)
        {
          using (new SecurityDisabler())
          {
            using (new EditContext(item, true, false))
            {
              item.Appearance.Sortorder = num;
            }
          }
          num += 100;
        }
      }
    }

    /// <summary>
    /// Gets the items.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="query">The query.</param>
    /// <returns>The items.</returns>
    private Item[] GetItemsToSort(Item item, string query)
    {
      Assert.ArgumentNotNull(item, "item");
      Assert.IsNotNullOrEmpty(query, "query");
      try
      {
        using (new LanguageSwitcher(item.Language))
        {
          if (query.StartsWith("fast:"))
          {
            return item.Database.SelectItems(query.Substring(5)) ?? new Item[0];
          }
          return item.Axes.SelectItems(query) ?? new Item[0];
        }
      }
      catch (Exception exception)
      {
        Item[] result = new Item[0];
        Log.Error("Failed to execute query:" + query, exception, this);
        return result;
      }
    }
  }
}