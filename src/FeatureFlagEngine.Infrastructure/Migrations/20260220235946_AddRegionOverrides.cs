using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeatureFlagEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegionOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeatureFlagName = table.Column<string>(type: "TEXT", nullable: false),
                    RegionId = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionOverrides_FeatureFlags_FeatureFlagName",
                        column: x => x.FeatureFlagName,
                        principalTable: "FeatureFlags",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegionOverrides_FeatureFlagName_RegionId",
                table: "RegionOverrides",
                columns: new[] { "FeatureFlagName", "RegionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegionOverrides");
        }
    }
}
