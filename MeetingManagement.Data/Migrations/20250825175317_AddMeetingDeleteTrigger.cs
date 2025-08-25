using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingDeleteTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create trigger to log deleted meetings
            migrationBuilder.Sql(@"
                CREATE TRIGGER TR_Meetings_AfterDelete
                ON Meetings
                AFTER DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    INSERT INTO MeetingLogs (
                        MeetingId,
                        Title,
                        Description,
                        StartDate,
                        EndDate,
                        DocumentPath,
                        UserId,
                        Operation,
                        LoggedAt,
                        LoggedBy
                    )
                    SELECT 
                        d.Id,
                        d.Title,
                        d.Description,
                        d.StartDate,
                        d.EndDate,
                        d.DocumentPath,
                        d.UserId,
                        'DELETE',
                        GETUTCDATE(),
                        SYSTEM_USER
                    FROM deleted d;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the trigger
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Meetings_AfterDelete");
        }
    }
}
