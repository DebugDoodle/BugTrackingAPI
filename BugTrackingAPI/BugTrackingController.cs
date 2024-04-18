using System.Data;
using Microsoft.AspNetCore.Mvc;
using Npgsql; 
using Dapper;
using System.Text.Json;

namespace BugTrackingController
{
    [Route("api")]
    [ApiController]
    public class BugTrackingController : ControllerBase
    {    public static string CONNECTION_STRING = "Server=62.169.26.82;Port=3306;Database=BugTracking;User Id=root;Password=Prajnan@1235;";

        [HttpGet, Route("Test")]
        public ActionResult Get()
        {
            return Ok("Hello from your API!");
        }

        [HttpGet, Route("FetchSection")]
        public ActionResult FetchSection()
        {
            try
            {
                string sectionResponse = string.Empty;
                string connectionString = CONNECTION_STRING;
                using (IDbConnection dbConnection = new MySqlConnector.MySqlConnection(connectionString)) 
                {
                    dbConnection.Open();
                    var bugSections = dbConnection.Query<BugSections>("SELECT * FROM BugSections");
                    sectionResponse = JsonSerializer.Serialize(bugSections);
                }
                return Ok(sectionResponse);
            }
            catch (Exception ex)
            {
                return Ok(ex.ToString());
            }

        }
         [HttpGet, Route("FetchEmployeeHierarchy")]
        public ActionResult FetchEmployeeHierarchy()
        {
            try
            {
                string hierarchyResponse = string.Empty;
                string connectionString = CONNECTION_STRING;
                using (IDbConnection dbConnection = new MySqlConnector.MySqlConnection(connectionString)) 
                {
                    dbConnection.Open();
                    var hierarchies = dbConnection.Query<EmployeeHierarchy>("SELECT EmployeeID AS id, CONCAT(FirstName, ' ', LastName) AS name FROM EmployeeHierarchy");
                    hierarchyResponse = JsonSerializer.Serialize(hierarchies);
                }
                return Ok(hierarchyResponse);
            }
            catch (Exception ex)
            {
                return Ok(ex.ToString());
            }

        }
        [HttpPost, Route("GetDashboard")]
        public ActionResult GetDashBoard(BugRequest req)
        {
            DashBoard dashBoard = new DashBoard();
            try
            {
                string dashboardResponse = string.Empty;
                string connectionString = CONNECTION_STRING;
                using (IDbConnection dbConnection = new MySqlConnector.MySqlConnection(connectionString)) 
                {
                    dbConnection.Open();
                    var TotalTickets = dbConnection.QuerySingle<int>($"Select count(*) from Bugs where Assignee = '{req.EmployeeId}'");
                    dashBoard.TotalTickets = TotalTickets.ToString();
                    var OpenTickets = dbConnection.QuerySingle<int>($"Select count(*) from Bugs where Assignee = '{req.EmployeeId}' and status = 'Open'");
                    dashBoard.OpenTickets = OpenTickets.ToString();
                    var ClosedTickets = dbConnection.QuerySingle<int>($"Select count(*) from Bugs where Assignee = '{req.EmployeeId}' and status = 'Closed'");
                    dashBoard.ClosedTickets = ClosedTickets.ToString();
                    var TicketsFixed = dbConnection.QuerySingle<int>($"Select count(*) from Bugs where Assignee = '{req.EmployeeId}' and status in ('Fixed','Closed')");
                    decimal ticketsFixedPercentage = ((decimal)TicketsFixed/TotalTickets)*100;
                    dashBoard.TicketsFixed = ticketsFixedPercentage.ToString();
                    dashboardResponse = JsonSerializer.Serialize(dashBoard);
                }
                return Ok(dashboardResponse);
            }
            catch (Exception ex)
            {
                return Ok(ex.ToString());
            }
        }
        [HttpPost, Route("BugData")]
        public ActionResult BugData(BugRequest req)
        {
            DashBoard dashBoard = new DashBoard();
            try
            {
                string dashboardResponse = string.Empty;
                string connectionString = CONNECTION_STRING;
                using (IDbConnection dbConnection = new MySqlConnector.MySqlConnection(connectionString)) 
                {
                    dbConnection.Open();
                    var bugSections = dbConnection.Query<DashBoard.OpenTicketsDetails>("p_fetchBugsData", new { EmpID = req.EmployeeId }, commandType: CommandType.StoredProcedure).ToList();
                    foreach(var bug in bugSections)
                    {
                        dashBoard.OpenTicketsList.Add(bug);
                    }
                    dashboardResponse = JsonSerializer.Serialize(dashBoard);
                }
                return Ok(dashboardResponse);
            }
            catch (Exception ex)
            {
                return Ok(ex.ToString());
            }
        }
       [HttpGet, Route("FetchProjectDetails")]
        public ActionResult FetchProjectDetails()
        {
            Projects projectData = new Projects();
            try
            {
                string sectionResponse = string.Empty;
                string connectionString = CONNECTION_STRING;
                using (IDbConnection dbConnection = new MySqlConnector.MySqlConnection(connectionString)) 
                {
                    dbConnection.Open();
                    var projects = dbConnection.Query<Project>("p_fetchProjects", commandType: CommandType.StoredProcedure).ToList();
                    var teamMember = dbConnection.Query<TeamMember>("p_fetchTeamMembers", commandType: CommandType.StoredProcedure).ToList();
                    foreach(var project in projects)
                    {
                       project.TeamMembers = teamMember
                        .Where(teamMember => teamMember.ProjectID == project.Id)
                        .ToList();
                    }
                    projectData.Projectdetails = projects;
                    sectionResponse = JsonSerializer.Serialize(projectData);
                }
                return Ok(sectionResponse);
            }
            catch (Exception ex)
            {
                return Ok(ex.ToString());
            }

        }
      
        [HttpPost, Route("CreateTicket")]
        public ActionResult CreateTicket(BugRequest req)
{
    try
    {
        string sectionResponse = string.Empty;
        string connectionString = CONNECTION_STRING;
        using (IDbConnection dbConnection = new MySqlConnector.MySqlConnection(connectionString)) 
        {
            dbConnection.Open();

            // Assign values from the request to variables
            string module = req.Module ?? "";
            string ticketTitle = req.TicketTitle ?? "";
            string assignee = req.Assignee ?? "";
            string priority = req.Priority ?? "";

            // Execute the SQL query with the assigned values
            var bugSections = dbConnection.Execute(
                @"INSERT INTO bugs (Module, TicketTitle, Status, Priority, Assignee, Attachment) 
                VALUES (@Module, @TicketTitle, 'Open', @Priority, @Assignee, '')",
                new { Module = module, TicketTitle = ticketTitle, Priority = priority, Assignee = assignee });

            // Serialize the response
            sectionResponse = JsonSerializer.Serialize(bugSections);
        }
        return Ok(sectionResponse);
    }
    catch (Exception ex)
    {
        return Ok(ex.ToString());
    }
}

        public class BugRequest 
        {
            public string? EmployeeId{get;set;}
            public string? Module { get; set; }
            public string? TicketTitle { get; set; }
            public string? Assignee { get; set; }
            public string? Priority { get; set; }
        }
        public class TeamMember
        {
        public int Id { get; set; }
        public int ProjectID { get; set; }
        public int EmployeeId { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? AvatarSrc { get; set; }
        }
        public class Projects
        {
            public List<Project> Projectdetails {get;set;} = new List<Project>();
        }

        public class Project
        {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ProjectLogo { get; set; }
        public List<TeamMember>? TeamMembers { get; set; }
        }

        public class DashBoard
        {
            public string? TotalTickets{get;set;}
            public string? ClosedTickets{get;set;}
            public string? OpenTickets{get;set;}
            public string? TicketsFixed{get;set;}
            public List<OpenTicketsDetails> OpenTicketsList{get;set;} = new List<OpenTicketsDetails>();
            public class OpenTicketsDetails
            {
                public string? Key{get;set;}
                public string? ID{get;set;}
                public string? Module{get;set;}
                public string? TicketTitle{get;set;}
                public string? Status{get;set;}
                public string? Priority{get;set;}
                public string? Assignee{get;set;}
                public byte[]? Attachment{get;set;}
            }
        }
        public class BugSections
        {
            public int Id { get; set; }
            public string? Type { get; set; }
            public string? Section { get; set; }
            public string? LogoURL { get; set; }
        }
        public class EmployeeHierarchy
        {
            public int id { get; set; }
            public string? name { get; set; }
        }
    }
}
