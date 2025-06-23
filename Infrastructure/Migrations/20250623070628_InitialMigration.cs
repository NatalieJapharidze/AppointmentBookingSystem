using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    specialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    appointment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    parent_appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.id);
                    table.ForeignKey(
                        name: "FK_appointments_appointments_parent_appointment_id",
                        column: x => x.parent_appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_appointments_service_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "service_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "blocked_times",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocked_times", x => x.id);
                    table.ForeignKey(
                        name: "FK_blocked_times_service_providers_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "service_providers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_blocked_times_service_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "service_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "working_hours",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_working_hours", x => x.id);
                    table.ForeignKey(
                        name: "FK_working_hours_service_providers_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "service_providers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_working_hours_service_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "service_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_logs_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_customer_email",
                table: "appointments",
                column: "customer_email");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_date_status",
                table: "appointments",
                columns: new[] { "appointment_date", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_parent_appointment_id",
                table: "appointments",
                column: "parent_appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_provider_date_time",
                table: "appointments",
                columns: new[] { "provider_id", "appointment_date", "start_time" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_status",
                table: "appointments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_blocked_times_provider_id_start_datetime_end_datetime",
                table: "blocked_times",
                columns: new[] { "provider_id", "start_datetime", "end_datetime" });

            migrationBuilder.CreateIndex(
                name: "IX_blocked_times_ServiceProviderId",
                table: "blocked_times",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_appointment_id",
                table: "notification_logs",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_sent_at",
                table: "notification_logs",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_status_retry_count",
                table: "notification_logs",
                columns: new[] { "status", "retry_count" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_type_status",
                table: "notification_logs",
                columns: new[] { "type", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_service_providers_email_unique",
                table: "service_providers",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_providers_is_active",
                table: "service_providers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_service_providers_specialty",
                table: "service_providers",
                column: "specialty");

            migrationBuilder.CreateIndex(
                name: "IX_working_hours_provider_id_day_of_week_is_active",
                table: "working_hours",
                columns: new[] { "provider_id", "day_of_week", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_working_hours_ServiceProviderId",
                table: "working_hours",
                column: "ServiceProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocked_times");

            migrationBuilder.DropTable(
                name: "notification_logs");

            migrationBuilder.DropTable(
                name: "working_hours");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "service_providers");
        }
    }
}
