using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMeetingDeleteTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Meetings_AfterDelete");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Trigger'ı geri eklemek için gerekli SQL
            migrationBuilder.Sql(@"
                CREATE TRIGGER TR_Meetings_AfterDelete
                ON Meetings
                AFTER DELETE
                AS
                BEGIN
                    INSERT INTO MeetingLogs (MeetingId, Action, ActionDate, UserId)
                    SELECT d.Id, 'Deleted', GETDATE(), d.CreatedBy
                    FROM deleted d;
                END
            ");
        }
    }
}
