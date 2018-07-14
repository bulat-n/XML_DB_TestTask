using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using log4net;

namespace XML_DB_TestTask.Controllers
{
    public class DBController : Controller
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const string DBName = "dbo.Files";

        public ActionResult Index()
        {
            Log.Debug("DBController.Index() started");
            return GetFilesDataFromDB();
        }

        [HttpGet]
        public ActionResult Update(string doAction, string id, string dateTime, string name, string version)
        {
            Log.Debug("Update() called. Action: " + doAction);
            if (String.IsNullOrWhiteSpace(id))
            {
                Log.Warn("Update() called with empty 'id'.");
                ViewBag.Message = "Wrong id";
            }
            else
            {
                switch(doAction)
                {
                    case "update":
                        ViewBag.Message = Update(Int32.Parse(id), name, version, dateTime);
                        break;
                    case "delete":
                        ViewBag.Message = Delete(Int32.Parse(id));
                        break;
                    default:
                        ViewBag.Message = "No action";
                        Log.Warn("Update() called with wrong 'doAction'.");
                        break;
                }
            }
            return View();
        }

        [HttpGet]
        public ActionResult Download(string id, string dateTime, string name, string version)
        {
            Log.Debug("Download() called.");
            string TempFolder = Server.MapPath("~/TempFiles");
            if (!Directory.Exists(TempFolder))
                Directory.CreateDirectory(TempFolder);

            var XMLData = new XElement("File",
                new XAttribute("FileVersion", version),
                new XElement("Name", name),
                new XElement("DateTime", dateTime)
                );

            string FileName = $"Файл_1_{id}.xml";
            string filePath = TempFolder + $"/{FileName}";
            System.IO.File.WriteAllText(TempFolder + $"/{FileName}", XMLData.ToString());

            Log.Debug("Temp file created. Name: " + FileName);

            FileInfo file = new FileInfo(filePath);
            Response.ClearContent();
            Response.AddHeader("Content-Disposition", "attachment; filename=" + file.Name);
            Response.AddHeader("Content-Length", file.Length.ToString());
            Response.ContentType = "text/plain";
            Response.TransmitFile(file.FullName);
            Response.End();

            Log.Debug("File sent to user.");

            System.IO.File.Delete(filePath);

            Log.Debug("File deleted.");

            return GetFilesDataFromDB();
        }
        
        public static void GetConnection(out SqlConnection connection, out SqlCommand command)
        {
            Log.Debug("GetConnection() called.");
            connection = new SqlConnection();
            connection.ConnectionString = @"Server=.;Database=FilesDB;Trusted_Connection=True;";
            command = new SqlCommand();
            command.Connection = connection;
            command.CommandType = CommandType.Text;
        }

        public ActionResult GetFilesDataFromDB()
        {
            Log.Debug("GetFilesDataFromDB() called.");
            GetConnection(out SqlConnection connection, out SqlCommand command);
            connection.Open();

            List<FilesModel> AllDataFromDB = new List<FilesModel>();
            try
            {
                command.CommandText = "SELECT * FROM " + DBName;
                Log.Debug("CommandText: " + command.CommandText);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        FilesModel FileData = new FilesModel
                        {
                            Id = reader.GetInt32(0),
                            DateTime = reader.GetString(1),
                            Name = reader.GetString(2),
                            Version = reader.GetString(3)
                        };
                        AllDataFromDB.Add(FileData);
                    }
                }
                else
                    Log.Warn("DB is empty.");
            }
            catch (Exception ex)
            {
                Log.Error("GetFilesDataFromDB() exception: " + ex.Message, ex);
            }
            finally
            {
                connection.Close();
            }
            return View(AllDataFromDB);
        }

        public string Update(int Id, string Name, string Version, string DateTime)
        {
            Log.Debug("Update() called.");
            GetConnection(out SqlConnection connection, out SqlCommand command);
            connection.Open();

            int Answer = 0;
            try
            {
                command.CommandText = $"UPDATE {DBName} SET Name = '{Name}', Version = '{Version}', DateTime = '{DateTime}' WHERE Id = {Id}";
                Log.Debug("CommandText: " + command.CommandText);
                Answer = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error("Update() exception: " + ex.Message, ex);
                return ex.Message;
            }
            finally
            {
                connection.Close();
            }
            //Not updated.
            if (Answer == 0)
            {
                Log.Warn("Update() failed.");
                return "Unknown error";
            }
            //Updated.
            else
                return "true";
        }

        public string Delete (int Id)
        {
            Log.Debug("Delete() called.");
            GetConnection(out SqlConnection connection, out SqlCommand command);
            connection.Open();

            int Answer = 0;
            try
            {
                command.CommandText = $"DELETE FROM {DBName} WHERE Id = {Id}";
                Log.Debug("CommandText: " + command.CommandText);
                Answer = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error("Delete() exception: " + ex.Message, ex);
                return ex.Message;
            }
            finally
            {
                connection.Close();
            }
            //Not removed.
            if (Answer == 0)
            {
                Log.Warn("Delete() failed.");
                return "Unknown error";
            }
            //Removed.
            else
                return "true";
        }

        public static string Insert(string Name, string Version, string DateTime)
        {
            Log.Debug("Insert() called.");
            GetConnection(out SqlConnection connection, out SqlCommand command);
            connection.Open();

            int Answer = 0;
            try
            {
                command.CommandText = $"INSERT INTO {DBName} (Name, Version, DateTime) VALUES ('{Name}', '{Version}', '{DateTime}')";
                Log.Debug("CommandText: " + command.CommandText);
                Answer = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error("Insert() exception: " + ex.Message, ex);
                return ex.Message;
            }
            finally
            {
                connection.Close();
            }
            if (Answer == 0)
            {
                Log.Warn("Insert() failed.");
                return "Unknown error";
            }
            else
                return "true";
        }
    }
}
