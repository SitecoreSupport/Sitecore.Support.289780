
namespace Sitecore.Support.XA.Foundation.RenderingVariants.Pipelines.RenderVariantField
{
  using Sitecore.Pipelines;
  using Sitecore.XA.Foundation.RenderingVariants.Fields;
  using Sitecore.XA.Foundation.RenderingVariants.Pipelines.RenderVariantField;
  using Sitecore.XA.Foundation.Variants.Abstractions.Fields;
  using Sitecore.XA.Foundation.Variants.Abstractions.Models;
  using Sitecore.XA.Foundation.Variants.Abstractions.Pipelines.RenderVariantField;
  using System;
  using System.Web.UI.HtmlControls;
  using Sitecore.Pipelines.RenderField;
  using Sitecore.Data.Items;
  using System.Web.UI;
  using System.Collections.Specialized;
  using System.Text.RegularExpressions;
  using System.Web.UI.WebControls;
  using Sitecore.Collections;
  using Sitecore.Data.Fields;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Links;
  using Sitecore.Resources.Media;
  using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;

  public class RenderSection : RenderRenderingVariantFieldProcessor
  {
    public override Type SupportedType
    {
      get
      {
        return typeof(VariantSection);
      }
    }

    public override RendererMode RendererMode
    {
      get
      {
        return RendererMode.Html;
      }
    }

    public override void RenderField(RenderVariantFieldArgs args)
    {
      VariantSection variantSection = args.VariantField as VariantSection;
      if (variantSection != null)
      {
        HtmlGenericControl htmlGenericControl = new HtmlGenericControl(string.IsNullOrWhiteSpace(variantSection.Tag) ? "div" : variantSection.Tag);
        this.AddClass(htmlGenericControl, variantSection.CssClass);
        this.AddWrapperDataAttributes(variantSection, args, htmlGenericControl);
        foreach (BaseVariantField sectionField in variantSection.SectionFields)
        {
          RenderVariantFieldArgs renderVariantFieldArgs = new RenderVariantFieldArgs
          {
            VariantField = sectionField,
            Item = args.Item,
            HtmlHelper = args.HtmlHelper,
            IsControlEditable = args.IsControlEditable,
            IsFromComposite = args.IsFromComposite,
            RendererMode = args.RendererMode,
            Model = args.Model
          };
          CorePipeline.Run("renderVariantField", renderVariantFieldArgs);
          if (renderVariantFieldArgs.ResultControl != null)
          {
            htmlGenericControl.Controls.Add(renderVariantFieldArgs.ResultControl);
          }
        }
        args.ResultControl = (variantSection.IsLink ? this.InsertHyperLink(htmlGenericControl, args.Item, variantSection.LinkAttributes, variantSection.LinkField, false, args.HrefOverrideFunc) : htmlGenericControl);
        args.Result = this.RenderControl(args.ResultControl);
      }
    }
    protected override Control InsertHyperLink(Control shownControl, Item item, NameValueCollection attributes, string linkFieldName, bool isDownloadLink, Func<Item, string, string> hrefOverrideFunc = null)
    {
      Control control = this.CreateHyperLink(item, linkFieldName, isDownloadLink, attributes, hrefOverrideFunc);
      this.MoveControl(shownControl, control);
      return control;
    }
    protected override Control CreateHyperLink(Item item, string linkFieldName, bool isDownloadLink, NameValueCollection attributes, Func<Item, string, string> hrefOverrideFunc)
    {
      UrlOptions defaultUrlOptions = this.LinkProviderService.GetLinkProvider(Context.Site).GetDefaultUrlOptions();
      defaultUrlOptions.Language = item.Language;
      if (hrefOverrideFunc != null)
      {
        string text = hrefOverrideFunc(item, linkFieldName);
        if (text != null)
        {
          return this.CreateHyperLink(text, item, isDownloadLink, attributes);
        }
      }
      if (!string.IsNullOrWhiteSpace(linkFieldName))
      {
        Field field = item.Fields[linkFieldName];
        if (field != null)
        {
          CustomField field2 = FieldTypeManager.GetField(field);
          if (field2 is FileField)
          {
            FileField fileField = item.Fields[linkFieldName];
            string href = (fileField.MediaItem != null) ? MediaManager.GetMediaUrl(fileField.MediaItem, new MediaUrlOptions
            {
              Language = fileField.MediaItem.Language
            }) : string.Empty;
            return this.CreateHyperLink(href, item, isDownloadLink, attributes);
          }
          if (field2 is Data.Fields.ImageField)
          {
            Data.Fields.ImageField imageField = item.Fields[linkFieldName];
            string href2 = (imageField.MediaItem != null) ? MediaManager.GetMediaUrl(imageField.MediaItem, new MediaUrlOptions
            {
              Language = imageField.MediaItem.Language
            }) : string.Empty;
            return this.CreateHyperLink(href2, item, isDownloadLink, attributes);
          }
          if (field2 is ReferenceField)
          {
            ReferenceField referenceField = item.Fields[linkFieldName];
            if (referenceField.TargetItem == null)// Patch for 289780
            {
              return this.CreateHyperLink("#", item, isDownloadLink, attributes);
            }// End Patch for 289780
            defaultUrlOptions.Language = referenceField.TargetItem.Language;
            return this.CreateHyperLink(LinkManager.GetItemUrl(referenceField.TargetItem, defaultUrlOptions), item, isDownloadLink, attributes);
          }
          if (field2 is LinkField)
          {
            RenderFieldArgs renderFieldArgs = new RenderFieldArgs
            {
              Item = item,
              FieldName = linkFieldName,
              Parameters =
                    {
                        ["haschildren"] = "true"
                    }
            };
            if (isDownloadLink)
            {
              ((SafeDictionary<string, string>)renderFieldArgs.Parameters)["download"] = "";
            }
            CorePipeline.Run("renderField", renderFieldArgs);
            return new RenderFieldControl(renderFieldArgs.Result);
          }
        }
      }
      if (item.IsMediaItem())
      {
        return this.CreateHyperLink(MediaManager.GetMediaUrl(item, new MediaUrlOptions
        {
          Language = item.Language
        }), item, isDownloadLink, attributes);
      }
      return this.CreateHyperLink(LinkManager.GetItemUrl(item, defaultUrlOptions), item, isDownloadLink, attributes);
    }
  }
}