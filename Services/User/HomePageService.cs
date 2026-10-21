
using DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class HomePageService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public HomePageService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        public async Task<HomePageVM> GetHomePageDataAsync(
            Guid pageId, string ip)
        {
            var result = new HomePageVM();

            var sectionsById =
                new Dictionary<Guid, HomePageSectionVM>();

            try
            {
                using (var connection =
                    new SqlConnection(_connectionString))
                using (var command =
                    new SqlCommand("dbo.sp_GetHomePageData", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(
                        "@PageId", SqlDbType.UniqueIdentifier).Value = pageId;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Result set 1: Sections
                        while (await reader.ReadAsync())
                        {
                            Guid sectionId =
                                reader.GetGuid(
                                    reader.GetOrdinal("SectionId"));

                            var section = new HomePageSectionVM
                            {
                                PageSectionId =
                                    reader.GetGuid(
                                        reader.GetOrdinal("PageSectionId")),

                                SectionId = sectionId,

                                PageSectionOrder =
                                    reader.GetInt32(
                                        reader.GetOrdinal("PageSectionOrder")),

                                Section = new SectionVM
                                {
                                    Id = sectionId,

                                    SectionTypeId =
                                        reader.GetInt32(
                                            reader.GetOrdinal("SectionTypeId")),

                                    Title = GetString(reader, "Title"),
                                    Heading = GetString(reader, "Heading"),
                                    SubHeading = GetString(reader, "SubHeading"),
                                    Description = GetString(reader, "Description"),
                                    InternalName = GetString(reader, "InternalName"),
                                    BackgroundColor = GetString(reader, "BackgroundColor"),
                                    CssClass = GetString(reader, "CssClass"),
                                    SettingsJson = GetString(reader, "SettingsJson"),
                                    Status = reader.GetBoolean(
                                        reader.GetOrdinal("Status"))
                                }
                            };

                            result.PageId =
                                reader.GetGuid(reader.GetOrdinal("PageId"));

                            result.PageTitle =
                                GetString(reader, "PageTitle");

                            result.Sections.Add(section);
                            sectionsById[sectionId] = section;
                        }

                        // Result set 2: Section items
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new SectionItemVM
                                {
                                    Id = reader.GetGuid(
                                        reader.GetOrdinal("Id")),

                                    SectionId = reader.GetGuid(
                                        reader.GetOrdinal("SectionId")),

                                    ParentSectionItemId = GetNullableGuid(
                                        reader, "ParentSectionItemId"),

                                    Title = GetString(reader, "Title"),
                                    Heading = GetString(reader, "Heading"),
                                    SubHeading = GetString(reader, "SubHeading"),
                                    Description = GetString(reader, "Description"),
                                    Value = GetString(reader, "Value"),
                                    Label = GetString(reader, "Label"),
                                    LinkUrl = GetString(reader, "LinkUrl"),
                                    AdditionalData = GetString(reader, "AdditionalData"),

                                    DisplayOrder = reader.GetInt32(
                                        reader.GetOrdinal("DisplayOrder")),

                                    Status = reader.GetBoolean(
                                        reader.GetOrdinal("Status"))
                                };

                                if (sectionsById.TryGetValue(
                                    item.SectionId, out var section))
                                {
                                    section.Items.Add(item);
                                }
                            }
                        }

                        // Result set 3: Images
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var image = new ImageVM
                                {
                                    Id = reader.GetGuid(
                                        reader.GetOrdinal("Id")),

                                    SectionId = GetNullableGuid(
                                        reader, "SectionId"),

                                    SectionItemId = GetNullableGuid(
                                        reader, "SectionItemId"),

                                    ImageUrl = GetString(reader, "ImageUrl"),
                                    AltText = GetString(reader, "AltText"),
                                    Caption = GetString(reader, "Caption"),
                                    ImageType = GetString(reader, "ImageType"),

                                    DisplayOrder = reader.GetInt32(
                                        reader.GetOrdinal("DisplayOrder")),

                                    Status = reader.GetBoolean(
                                        reader.GetOrdinal("Status"))
                                };

                                // Section-level image
                                if (image.SectionId.HasValue &&
                                    sectionsById.TryGetValue(
                                        image.SectionId.Value,
                                        out var section))
                                {
                                    section.Images.Add(image);
                                }

                                // Section-item-level image
                                if (image.SectionItemId.HasValue)
                                {
                                    foreach (var homeSection in result.Sections)
                                    {
                                        var item = homeSection.Items.FirstOrDefault(
                                            i => i.Id == image.SectionItemId.Value);

                                        if (item != null)
                                        {
                                            item.Images.Add(image);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        // Result set 4: Buttons
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var button = new ButtonVM
                                {
                                    Id = reader.GetGuid(
                                        reader.GetOrdinal("Id")),

                                    SectionId = GetNullableGuid(
                                        reader, "SectionId"),

                                    SectionItemId = GetNullableGuid(
                                        reader, "SectionItemId"),

                                    Title = GetString(reader, "Title"),
                                    Url = GetString(reader, "Url"),
                                    Target = GetString(reader, "Target"),
                                    CssClass = GetString(reader, "CssClass"),
                                    ButtonType = GetString(reader, "ButtonType"),

                                    DisplayOrder = reader.GetInt32(
                                        reader.GetOrdinal("DisplayOrder")),

                                    Status = reader.GetBoolean(
                                        reader.GetOrdinal("Status"))
                                };

                                if (button.SectionId.HasValue &&
                                    sectionsById.TryGetValue(
                                        button.SectionId.Value,
                                        out var section))
                                {
                                    section.Buttons.Add(button);
                                }
                            }
                        }

                        // Result set 5: Testimonials
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Testimonials.Add(
                                    new TestimonialVM
                                    {
                                        Id = reader.GetGuid(
                                            reader.GetOrdinal("Id")),

                                        ClientName = GetString(
                                            reader, "ClientName"),

                                        ReviewText = GetString(
                                            reader, "ReviewText"),

                                        Rating = reader.GetInt32(
                                            reader.GetOrdinal("Rating")),

                                        DisplayOrder = reader.GetInt32(
                                            reader.GetOrdinal("DisplayOrder")),

                                        Status = true
                                    });
                            }
                        }
                    }
                }

                result.Sections.Sort(
                    (a, b) => a.PageSectionOrder.CompareTo(
                        b.PageSectionOrder));

                return result;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Home/Index",
                    "GET",
                    0,
                    ip,
                    "sp_GetHomePageData");

                throw;
            }
        }

        private static string GetString(
            SqlDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }

        private static Guid? GetNullableGuid(
            SqlDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? (Guid?)null
                : reader.GetGuid(ordinal);
        }
    }
}
