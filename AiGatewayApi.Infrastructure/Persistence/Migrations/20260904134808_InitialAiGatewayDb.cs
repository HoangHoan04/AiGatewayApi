using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiGatewayApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAiGatewayDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: true),
                    ApiKeyEncrypted = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    TimeoutMs = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    HeadersJson = table.Column<string>(type: "jsonb", nullable: true),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AzureDeployment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDefaultFallback = table.Column<bool>(type: "boolean", nullable: false),
                    LastHealthAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HealthStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_models",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InputPricePer1K = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    OutputPricePer1K = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    OutputPricePer1KCached = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    PriceUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MaxContextTokens = table.Column<int>(type: "integer", nullable: false),
                    SupportsStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    Capabilities = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeprecatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_models", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_models_ai_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "ai_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "provider_health_checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LatencyMs = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ModelCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_health_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_provider_health_checks_ai_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "ai_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                    AllowedProviderTypes = table.Column<string>(type: "jsonb", nullable: true),
                    CallbackWebhook = table.Column<string>(type: "text", nullable: true),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_projects_ai_models_DefaultModelId",
                        column: x => x.DefaultModelId,
                        principalTable: "ai_models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    KeyPrefix = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AllowedModels = table.Column<string>(type: "jsonb", nullable: true),
                    Scopes = table.Column<string>(type: "jsonb", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RateLimitRpm = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_api_keys_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BlockSecrets = table.Column<bool>(type: "boolean", nullable: false),
                    MaxPromptChars = table.Column<int>(type: "integer", nullable: false),
                    StorePrompts = table.Column<bool>(type: "boolean", nullable: false),
                    PromptRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    BlockedPatternsJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_policies_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quota_usages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Tokens = table.Column<long>(type: "bigint", nullable: false),
                    Requests = table.Column<int>(type: "integer", nullable: false),
                    CostUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quota_usages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quota_usages_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenLimit = table.Column<long>(type: "bigint", nullable: true),
                    RequestLimit = table.Column<int>(type: "integer", nullable: true),
                    RateLimitRpm = table.Column<int>(type: "integer", nullable: true),
                    RateLimitTpd = table.Column<long>(type: "bigint", nullable: true),
                    CostLimitUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    SoftLimit = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentMonthTokens = table.Column<long>(type: "bigint", nullable: false),
                    CurrentMonthRequests = table.Column<int>(type: "integer", nullable: false),
                    CurrentMonthCostUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    AlertThreshold = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AlertWebhook = table.Column<string>(type: "text", nullable: true),
                    LastAlertedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotas_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "routing_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ConditionJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_routing_policies_ai_models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "ai_models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_routing_policies_ai_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "ai_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_routing_policies_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "async_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApiKeyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    InputRef = table.Column<string>(type: "text", nullable: true),
                    ResultRef = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CallbackWebhook = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_async_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_async_jobs_api_keys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "api_keys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_async_jobs_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prompt_template_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    UserPromptTemplate = table.Column<string>(type: "text", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeNote = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_template_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "prompt_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SourceSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    VariablesSchemaJson = table.Column<string>(type: "jsonb", nullable: true),
                    PublishedVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompt_templates_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prompt_templates_prompt_template_versions_PublishedVersionId",
                        column: x => x.PublishedVersionId,
                        principalTable: "prompt_template_versions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "usage_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FallbackFromProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PromptTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    CostUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    LatencyMs = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    RequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    IsBillable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usage_logs_ai_models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "ai_models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usage_logs_api_keys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "api_keys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usage_logs_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usage_logs_prompt_templates_PromptTemplateId",
                        column: x => x.PromptTemplateId,
                        principalTable: "prompt_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_models_ProviderId_ModelCode",
                table: "ai_models",
                columns: new[] { "ProviderId", "ModelCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_providers_ProviderType",
                table: "ai_providers",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_KeyPrefix",
                table: "api_keys",
                column: "KeyPrefix");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_ProjectId",
                table: "api_keys",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_async_jobs_ApiKeyId",
                table: "async_jobs",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_async_jobs_ProjectId",
                table: "async_jobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_async_jobs_Status",
                table: "async_jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_content_policies_ProjectId",
                table: "content_policies",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_Code",
                table: "projects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_CompanyId",
                table: "projects",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_DefaultModelId",
                table: "projects",
                column: "DefaultModelId");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_template_versions_TemplateId_VersionNumber",
                table: "prompt_template_versions",
                columns: new[] { "TemplateId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompt_templates_Code",
                table: "prompt_templates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompt_templates_ProjectId",
                table: "prompt_templates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_templates_PublishedVersionId",
                table: "prompt_templates",
                column: "PublishedVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_health_checks_CreatedAt",
                table: "provider_health_checks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_provider_health_checks_ProviderId",
                table: "provider_health_checks",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_quota_usages_ProjectId_Period_PeriodStart",
                table: "quota_usages",
                columns: new[] { "ProjectId", "Period", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotas_ProjectId",
                table: "quotas",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_routing_policies_ModelId",
                table: "routing_policies",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_routing_policies_ProjectId_Priority",
                table: "routing_policies",
                columns: new[] { "ProjectId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_routing_policies_ProviderId",
                table: "routing_policies",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_logs_ApiKeyId",
                table: "usage_logs",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_logs_CompanyId",
                table: "usage_logs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_logs_CreatedAt",
                table: "usage_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_usage_logs_ModelId",
                table: "usage_logs",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_logs_ProjectId",
                table: "usage_logs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_logs_PromptTemplateId",
                table: "usage_logs",
                column: "PromptTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_prompt_template_versions_prompt_templates_TemplateId",
                table: "prompt_template_versions",
                column: "TemplateId",
                principalTable: "prompt_templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_models_ai_providers_ProviderId",
                table: "ai_models");

            migrationBuilder.DropForeignKey(
                name: "FK_prompt_templates_projects_ProjectId",
                table: "prompt_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_prompt_template_versions_prompt_templates_TemplateId",
                table: "prompt_template_versions");

            migrationBuilder.DropTable(
                name: "async_jobs");

            migrationBuilder.DropTable(
                name: "content_policies");

            migrationBuilder.DropTable(
                name: "provider_health_checks");

            migrationBuilder.DropTable(
                name: "quota_usages");

            migrationBuilder.DropTable(
                name: "quotas");

            migrationBuilder.DropTable(
                name: "routing_policies");

            migrationBuilder.DropTable(
                name: "usage_logs");

            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "ai_providers");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "ai_models");

            migrationBuilder.DropTable(
                name: "prompt_templates");

            migrationBuilder.DropTable(
                name: "prompt_template_versions");
        }
    }
}
