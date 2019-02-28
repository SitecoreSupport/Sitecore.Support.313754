using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Comparers;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Pipelines;
using Sitecore.Pipelines.ExpandInitialFieldValue;
using Sitecore.Resources;
using Sitecore.SecurityModel;
using Sitecore.Shell.Applications.Dialogs.SortContent;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;

namespace Sitecore.Shell.Applications.Dialogs.Sort
{
  /// <summary>
  /// The reorder.
  /// </summary>
  public class SortForm : DialogForm
  {
    /// <summary>
    /// The main container.
    /// </summary>
    protected Scrollbox MainContainer;

    /// <summary>
    /// The sort by;
    /// </summary>
    private string sortBy;

    /// <summary>
    /// Gets or sets a value indicating whether standard value tokens should be expanded
    /// </summary>
    private bool expandStandardValuesTokens;

    /// <summary>
    /// The on load.
    /// </summary>
    /// <param name="e">The e.</param>
    /// <remarks>
    /// This method notifies the server control that it should perform actions common to each HTTP
    /// request for the page it is associated with, such as setting up a database query. At this
    /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
    /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
    /// property to determine whether the page is being loaded in response to a client postback,
    /// or if it is being loaded and accessed for the first time.
    /// </remarks>
    protected override void OnLoad(EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent)
      {
        SortContentOptions sortContentOptions = SortContentOptions.Parse();
        sortBy = sortContentOptions.SortBy;
        expandStandardValuesTokens = sortContentOptions.ExpandStandardValuesTokens;
        string contentToSortQuery = sortContentOptions.ContentToSortQuery;
        Assert.IsNotNullOrEmpty(contentToSortQuery, "query");
        Item[] itemsToSort = GetItemsToSort(sortContentOptions.Item, contentToSortQuery);
        Array.Sort(itemsToSort, new DefaultComparer());
        if (itemsToSort.Length < 2)
        {
          OK.Disabled = true;
        }
        else
        {
          MainContainer.Controls.Clear();
          MainContainer.InnerHtml = Render(itemsToSort);
        }
      }
    }

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
    /// The render.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <returns>The render.</returns>
    private string Render(IEnumerable<Item> items)
    {
      Assert.ArgumentNotNull(items, "items");
      HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
      htmlTextWriter.Write("<ul id='sort-list'>");
      foreach (Item item in items)
      {
        Render(htmlTextWriter, item);
      }
      htmlTextWriter.Write("</ul>");
      return htmlTextWriter.InnerWriter.ToString();
    }

    /// <summary>
    /// Renders the specified writer.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="item">The item.</param>
    private void Render(HtmlTextWriter writer, Item item)
    {
      Assert.ArgumentNotNull(writer, "writer");
      Assert.ArgumentNotNull(item, "item");
      bool flag = IsEditable(item);
      string text = GetSortBy(item);
      string arg = (!flag) ? Translate.Text("You cannot edit this item because you do not have write access to it.") : text;
      writer.Write("<li id='{0}' class='sort-item {1}' title='{2}'>", item.ID.ToShortID(), flag ? "editable" : "non-editable", arg);
      writer.Write("<img src='/sitecore/shell/Themes/Standard/Images/draghandle9x15.png' class='drag-handle' />");
      writer.Write("<img src='{0}' class='item-icon' />", Images.GetThemedImageSource(item.Appearance.Icon, ImageDimension.id16x16));
      writer.Write("<span unselectable='on' class='item-name'>{0}</span>", StringUtil.Clip(text, 40, true));
      writer.Write("</li>");
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
            using (new EditContext(item, false, false))
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

    /// <summary>
    /// Gets the sort by.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The sort by.</returns>
    private string GetSortBy(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      if (string.IsNullOrEmpty(sortBy))
      {
        return item.GetUIDisplayName();
      }
      Field field = item.Fields[sortBy];
      if (field == null)
      {
        return Translate.Text("[none]");
      }
      if (expandStandardValuesTokens && field.ContainsStandardValue)
      {
        ExpandInitialFieldValueArgs expandInitialFieldValueArgs = new ExpandInitialFieldValueArgs(field, item);
        CorePipeline.Run("expandInitialFieldValue", expandInitialFieldValueArgs);
        return StringUtil.RemoveTags(expandInitialFieldValueArgs.Result ?? field.Value);
      }
      return StringUtil.RemoveTags(field.Value);
    }

    /// <summary>
    /// Determines whether the specified item is editable.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>
    /// 	<c>true</c> if the specified item is editable; otherwise, <c>false</c>.
    /// </returns>
    private static bool IsEditable(Item item)
    {
      Assert.IsNotNull(item, "item");
      if (!Context.IsAdministrator && item.Locking.IsLocked() && !item.Locking.HasLock())
      {
        return false;
      }
      if (item.Appearance.ReadOnly)
      {
        return false;
      }
      return item.Access.CanWrite();
    }
  }
}