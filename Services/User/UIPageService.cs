using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using DTOs;

namespace Services
{
    public class UIPageService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public UIPageService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        // Returns null when the page does not exist or is not published.
        public async Task<UIPageVM> GetPageContentAsync(Guid pageId, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(
                    "dbo.usp_GetPageContentByPageId", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(
                        "@PageId", SqlDbType.UniqueIdentifier).Value = pageId;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Result set 0: Page
                        UIPageVM page = null;

                        if (await reader.ReadAsync())
                        {
                            page = MapPage(reader);
                        }

                        if (page == null)
                        {
                            return null;
                        }

                        var sectionMap = new Dictionary<Guid, UISectionVM>();
                        var itemMap = new Dictionary<Guid, UISectionItemVM>();

                        // Result set 1: Sections
                        await reader.NextResultAsync();

                        while (await reader.ReadAsync())
                        {
                            var section = MapSection(reader);
                            page.Sections.Add(section);
                            sectionMap[section.Id] = section;
                        }

                        // Result set 2: Section items (top level)
                        await reader.NextResultAsync();

                        while (await reader.ReadAsync())
                        {
                            var item = MapItem(reader);
                            itemMap[item.Id] = item;

                            if (sectionMap.TryGetValue(item.SectionId, out var section))
                            {
                                section.Items.Add(item);
                            }
                        }

                        // Result set 3: Child items
                        await reader.NextResultAsync();

                        var children = new List<UISectionItemVM>();

                        while (await reader.ReadAsync())
                        {
                            var child = MapItem(reader);
                            children.Add(child);
                            itemMap[child.Id] = child;
                        }

                        foreach (var child in children)
                        {
                            if (child.ParentSectionItemId.HasValue &&
                                itemMap.TryGetValue(
                                    child.ParentSectionItemId.Value,
                                    out var parent))
                            {
                                parent.Children.Add(child);
                            }
                        }

                        // Result set 4: Section images
                        await reader.NextResultAsync();

                        while (await reader.ReadAsync())
                        {
                            var image = MapSectionImage(reader);

                            if (image.SectionId.HasValue &&
                                sectionMap.TryGetValue(image.SectionId.Value, out var section))
                            {
                                section.Images.Add(image);
                            }
                        }

                        // Result set 5: Section item images
                        await reader.NextResultAsync();

                        while (await reader.ReadAsync())
                        {
                            var image = MapItemImage(reader);

                            if (image.SectionItemId.HasValue &&
                                itemMap.TryGetValue(image.SectionItemId.Value, out var item))
                            {
                                item.Images.Add(image);
                            }
                        }

                        // Result set 6: Section buttons
                        await reader.NextResultAsync();

                        while (await reader.ReadAsync())
                        {
                            var button = MapButton(reader, false);

                            if (button.SectionId.HasValue &&
                                sectionMap.TryGetValue(button.SectionId.Value, out var section))
                            {
                                section.Buttons.Add(button);
                            }
                        }

                        // Result set 7: Section item buttons
                        await reader.NextResultAsync();

                        while (await reader.ReadAsync())
                        {
                            var button = MapButton(reader, true);

                            if (button.SectionItemId.HasValue &&
                                itemMap.TryGetValue(button.SectionItemId.Value, out var item))
                            {
                                item.Buttons.Add(button);
                            }
                        }

                        // Result set 8: Testimonials
                        await reader.NextResultAsync();

                        while (await reader.ReadAsync())
                        {
                            page.Testimonials.Add(MapTestimonial(reader));
                        }

                        SortByDisplayOrder(page, itemMap.Values);

                        return page;
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Home/Index", "GET", 0, ip, "GetPageContentAsync");

                throw;
            }
        }

        // The images, buttons and testimonials queries have no ORDER BY,
        // so the order is applied here.
        private static void SortByDisplayOrder(
            UIPageVM page, IEnumerable<UISectionItemVM> items)
        {
            foreach (var section in page.Sections)
            {
                section.Images = section.Images
                    .OrderBy(x => x.DisplayOrder).ToList();
                section.Buttons = section.Buttons
                    .OrderBy(x => x.DisplayOrder).ToList();
            }

            foreach (var item in items)
            {
                item.Images = item.Images
                    .OrderBy(x => x.DisplayOrder).ToList();
                item.Buttons = item.Buttons
                    .OrderBy(x => x.DisplayOrder).ToList();
            }

            page.Testimonials = page.Testimonials
                .OrderBy(x => x.DisplayOrder).ToList();
        }

        private static UIPageVM MapPage(SqlDataReader reader)
        {
            return new UIPageVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Title = GetString(reader, "Title"),
                Slug = GetString(reader, "Slug"),
                MetaTitle = GetString(reader, "MetaTitle"),
                MetaDescription = GetString(reader, "MetaDescription"),
                MetaKeywords = GetString(reader, "MetaKeywords"),
                CanonicalUrl = GetString(reader, "CanonicalUrl")
            };
        }

        private static UISectionVM MapSection(SqlDataReader reader)
        {
            return new UISectionVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                Title = GetString(reader, "Title"),
                Heading = GetString(reader, "Heading"),
                SubHeading = GetString(reader, "SubHeading"),
                Description = GetString(reader, "Description"),
                BackgroundColor = GetString(reader, "BackgroundColor"),
                CssClass = GetString(reader, "CssClass"),
                SettingsJson = GetString(reader, "SettingsJson")
            };
        }

        private static UISectionItemVM MapItem(SqlDataReader reader)
        {
            return new UISectionItemVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                SectionId = reader.GetGuid(reader.GetOrdinal("SectionId")),
                ParentSectionItemId = GetGuid(reader, "ParentSectionItemId"),
                Title = GetString(reader, "Title"),
                Heading = GetString(reader, "Heading"),
                SubHeading = GetString(reader, "SubHeading"),
                Description = GetString(reader, "Description"),
                Value = GetString(reader, "Value"),
                Label = GetString(reader, "Label"),
                LinkUrl = GetString(reader, "LinkUrl"),
                AdditionalData = GetString(reader, "AdditionalData")
            };
        }

        // Section image query returns SectionId (no SectionItemId).
        private static ImageVM MapSectionImage(SqlDataReader reader)
        {
            var image = MapImageCommon(reader);
            image.SectionId = GetGuid(reader, "SectionId");
            return image;
        }

        // Item image query returns SectionItemId (no SectionId).
        private static ImageVM MapItemImage(SqlDataReader reader)
        {
            var image = MapImageCommon(reader);
            image.SectionItemId = GetGuid(reader, "SectionItemId");
            return image;
        }

        private static ImageVM MapImageCommon(SqlDataReader reader)
        {
            return new ImageVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                ImageUrl = GetString(reader, "ImageUrl"),
                AltText = GetString(reader, "AltText"),
                Caption = GetString(reader, "Caption"),
                ImageType = GetString(reader, "ImageType"),
                DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder"))
            };
        }

        // The section button query has no SectionItemId column,
        // the item button query has both SectionId and SectionItemId.
        private static ButtonVM MapButton(SqlDataReader reader, bool hasItemId)
        {
            return new ButtonVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                SectionId = GetGuid(reader, "SectionId"),
                SectionItemId = hasItemId
                    ? GetGuid(reader, "SectionItemId")
                    : null,
                Title = GetString(reader, "Title"),
                Url = GetString(reader, "Url"),
                Target = GetString(reader, "Target"),
                CssClass = GetString(reader, "CssClass"),
                ButtonType = GetString(reader, "ButtonType"),
                DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder"))
            };
        }

        private static TestimonialVM MapTestimonial(SqlDataReader reader)
        {
            return new TestimonialVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                ClientName = GetString(reader, "ClientName"),
                ReviewText = GetString(reader, "ReviewText"),
                Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder"))
            };
        }

        private static Guid? GetGuid(SqlDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? (Guid?)null
                : reader.GetGuid(ordinal);
        }

        private static string GetString(SqlDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }
    }
}