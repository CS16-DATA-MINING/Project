using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionRecherche.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolet1CoreSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EstActif",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "GradeActuel",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LaboratoireId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UerId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DemandesPublication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Abstract = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuteurId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StatutValidationInterne = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "EnAttenteLabo"),
                    NumeroCycleValidation = table.Column<int>(type: "int", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DateAutorisationConseilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateSoumissionReelleUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NumeroSuivi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NomOriginalPreuve = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CheminStockagePreuve = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TypeContenuPreuve = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StatutEditeurCourant = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DateDerniereMiseAJourUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandesPublication", x => x.Id);
                    table.CheckConstraint("CK_DemandesPublication_NumeroCycle", "[NumeroCycleValidation] >= 1");
                    table.ForeignKey(
                        name: "FK_DemandesPublication_AspNetUsers_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Journaux",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomNormalise = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Editeur = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Issn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssnNormalise = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Journaux", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParametresSysteme",
                columns: table => new
                {
                    Cle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Valeur = table.Column<int>(type: "int", nullable: false),
                    Unite = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DateDerniereModificationUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametresSysteme", x => x.Cle);
                    table.CheckConstraint("CK_ParametresSysteme_ValeurPositive", "[Valeur] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Uers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChefUerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EstActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Uers_AspNetUsers_ChefUerId",
                        column: x => x.ChefUerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anomalies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandePublicationId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Ouverte"),
                    DateDetectionUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DureeJours = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DateTraitementUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TraiteeParId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CommentaireTraitement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anomalies", x => x.Id);
                    table.CheckConstraint("CK_Anomalies_Duree", "[DureeJours] >= 0");
                    table.CheckConstraint("CK_Anomalies_Traitement", "([Statut] = N'Ouverte' AND [DateTraitementUtc] IS NULL AND [TraiteeParId] IS NULL) OR ([Statut] = N'Traitee' AND [DateTraitementUtc] IS NOT NULL AND [TraiteeParId] IS NOT NULL AND [CommentaireTraitement] IS NOT NULL AND LEN(LTRIM(RTRIM([CommentaireTraitement]))) > 0)");
                    table.ForeignKey(
                        name: "FK_Anomalies_AspNetUsers_TraiteeParId",
                        column: x => x.TraiteeParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anomalies_DemandesPublication_DemandePublicationId",
                        column: x => x.DemandePublicationId,
                        principalTable: "DemandesPublication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EtapesValidation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandePublicationId = table.Column<int>(type: "int", nullable: false),
                    ValidateurId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Niveau = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateDecisionUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    NumeroCycleValidation = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapesValidation", x => x.Id);
                    table.CheckConstraint("CK_EtapesValidation_CommentaireRejet", "([Decision] <> N'Rejetee') OR ([Commentaire] IS NOT NULL AND LEN(LTRIM(RTRIM([Commentaire]))) > 0)");
                    table.ForeignKey(
                        name: "FK_EtapesValidation_AspNetUsers_ValidateurId",
                        column: x => x.ValidateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EtapesValidation_DemandesPublication_DemandePublicationId",
                        column: x => x.DemandePublicationId,
                        principalTable: "DemandesPublication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoriquesStatutEditeur",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandePublicationId = table.Column<int>(type: "int", nullable: false),
                    AncienStatut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NouveauStatut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DateChangementUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifieParId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriquesStatutEditeur", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoriquesStatutEditeur_AspNetUsers_ModifieParId",
                        column: x => x.ModifieParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoriquesStatutEditeur_DemandesPublication_DemandePublicationId",
                        column: x => x.DemandePublicationId,
                        principalTable: "DemandesPublication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JournauxCiblesDemande",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandePublicationId = table.Column<int>(type: "int", nullable: false),
                    JournalId = table.Column<int>(type: "int", nullable: true),
                    EstNouveauJournal = table.Column<bool>(type: "bit", nullable: false),
                    NomJournalSaisiManuellement = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NomJournalNormalise = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EditeurSaisiManuellement = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssnSaisiManuellement = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IssnNormalise = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OrdrePreference = table.Column<int>(type: "int", nullable: false),
                    StatutModeration = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "NonRequise"),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournauxCiblesDemande", x => x.Id);
                    table.CheckConstraint("CK_JournauxCiblesDemande_Ordre", "[OrdrePreference] >= 1");
                    table.CheckConstraint("CK_JournauxCiblesDemande_Source", "([EstNouveauJournal] = 0 AND [JournalId] IS NOT NULL) OR ([EstNouveauJournal] = 1 AND [NomJournalSaisiManuellement] IS NOT NULL AND [EditeurSaisiManuellement] IS NOT NULL AND [IssnSaisiManuellement] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_JournauxCiblesDemande_DemandesPublication_DemandePublicationId",
                        column: x => x.DemandePublicationId,
                        principalTable: "DemandesPublication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JournauxCiblesDemande_Journaux_JournalId",
                        column: x => x.JournalId,
                        principalTable: "Journaux",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Laboratoires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UerId = table.Column<int>(type: "int", nullable: false),
                    ChefLaboId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChefLaboId1 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Laboratoires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Laboratoires_AspNetUsers_ChefLaboId1",
                        column: x => x.ChefLaboId1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Laboratoires_Uers_UerId",
                        column: x => x.UerId,
                        principalTable: "Uers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ParametresSysteme",
                columns: new[] { "Cle", "DateDerniereModificationUtc", "Description", "Unite", "Valeur" },
                values: new object[,]
                {
                    { "D1", null, "Délai maximal avant déclaration de la soumission réelle.", "Jours", 30 },
                    { "D2", null, "Délai maximal sans évolution du statut éditeur.", "Mois", 6 },
                    { "MaxJournaux", null, "Nombre maximal de journaux ciblés par demande.", "Nombre", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LaboratoireId",
                table: "AspNetUsers",
                column: "LaboratoireId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UerId",
                table: "AspNetUsers",
                column: "UerId");

            migrationBuilder.CreateIndex(
                name: "IX_Anomalies_DateDetectionUtc",
                table: "Anomalies",
                column: "DateDetectionUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Anomalies_DemandePublicationId_Type",
                table: "Anomalies",
                columns: new[] { "DemandePublicationId", "Type" },
                unique: true,
                filter: "[Statut] = N'Ouverte'");

            migrationBuilder.CreateIndex(
                name: "IX_Anomalies_TraiteeParId",
                table: "Anomalies",
                column: "TraiteeParId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesPublication_AuteurId",
                table: "DemandesPublication",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesPublication_DateAutorisationConseilUtc",
                table: "DemandesPublication",
                column: "DateAutorisationConseilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesPublication_DateDerniereMiseAJourUtc",
                table: "DemandesPublication",
                column: "DateDerniereMiseAJourUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesPublication_StatutValidationInterne",
                table: "DemandesPublication",
                column: "StatutValidationInterne");

            migrationBuilder.CreateIndex(
                name: "IX_EtapesValidation_DateDecisionUtc",
                table: "EtapesValidation",
                column: "DateDecisionUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EtapesValidation_DemandePublicationId_NumeroCycleValidation_Niveau",
                table: "EtapesValidation",
                columns: new[] { "DemandePublicationId", "NumeroCycleValidation", "Niveau" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EtapesValidation_ValidateurId",
                table: "EtapesValidation",
                column: "ValidateurId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriquesStatutEditeur_DemandePublicationId_DateChangementUtc",
                table: "HistoriquesStatutEditeur",
                columns: new[] { "DemandePublicationId", "DateChangementUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoriquesStatutEditeur_ModifieParId",
                table: "HistoriquesStatutEditeur",
                column: "ModifieParId");

            migrationBuilder.CreateIndex(
                name: "IX_JournauxCiblesDemande_DemandePublicationId_OrdrePreference",
                table: "JournauxCiblesDemande",
                columns: new[] { "DemandePublicationId", "OrdrePreference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournauxCiblesDemande_IssnNormalise",
                table: "JournauxCiblesDemande",
                column: "IssnNormalise");

            migrationBuilder.CreateIndex(
                name: "IX_JournauxCiblesDemande_JournalId",
                table: "JournauxCiblesDemande",
                column: "JournalId");

            migrationBuilder.CreateIndex(
                name: "IX_Laboratoires_ChefLaboId1",
                table: "Laboratoires",
                column: "ChefLaboId1");

            migrationBuilder.CreateIndex(
                name: "IX_Laboratoires_UerId",
                table: "Laboratoires",
                column: "UerId");

            migrationBuilder.CreateIndex(
                name: "IX_Uers_ChefUerId",
                table: "Uers",
                column: "ChefUerId");

            migrationBuilder.CreateIndex(
                name: "IX_Uers_Code",
                table: "Uers",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Laboratoires_LaboratoireId",
                table: "AspNetUsers",
                column: "LaboratoireId",
                principalTable: "Laboratoires",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Uers_UerId",
                table: "AspNetUsers",
                column: "UerId",
                principalTable: "Uers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Laboratoires_LaboratoireId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Uers_UerId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Anomalies");

            migrationBuilder.DropTable(
                name: "EtapesValidation");

            migrationBuilder.DropTable(
                name: "HistoriquesStatutEditeur");

            migrationBuilder.DropTable(
                name: "JournauxCiblesDemande");

            migrationBuilder.DropTable(
                name: "Laboratoires");

            migrationBuilder.DropTable(
                name: "ParametresSysteme");

            migrationBuilder.DropTable(
                name: "DemandesPublication");

            migrationBuilder.DropTable(
                name: "Journaux");

            migrationBuilder.DropTable(
                name: "Uers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LaboratoireId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EstActif",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GradeActuel",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LaboratoireId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UerId",
                table: "AspNetUsers");
        }
    }
}
