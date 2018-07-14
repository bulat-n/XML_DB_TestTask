using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;
using System;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace XML_DB_TestTask.Controllers
{
    public class HomeController : Controller
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ActionResult Index()
        {
            Log.Debug("Index() is started");
            return View();
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
        {
            Log.Info("Receiving file from user.");
            // Verify that the user selected a file
            if (file != null && file.ContentLength > 0)
            {
                // extract only the filename
                var fileName = Path.GetFileName(file.FileName);
                if (!CheckFileName(fileName))
                {
                    Log.Info("Wrong file name.");
                    ViewBag.Message = "Имя файла имело неверный формат. Требования: XX_YY_ZZ.xml, где: XX – набор русских букв. Количество символов - не более 100; YY – набор цифр. Количество символов – либо 1, либо 10, либо от 14 до 20; ZZ – любые символы. Количество символов – не более 7.";
                    return View();
                }
                Log.Info("File name is right.");
                // store the file inside ~/Uploads folder
                var Folder = Server.MapPath("~/Uploads");
                if (!Directory.Exists(Folder))
                    Directory.CreateDirectory(Folder);
                var path = Path.Combine(Folder, fileName);
                file.SaveAs(path);
                Log.Info("File saved.");

                try
                {
                    XmlDocument document = new XmlDocument();
                    document.Load(path);
                    var Version = document.SelectSingleNode("/File").Attributes["FileVersion"].Value;
                    var FileName = document.SelectSingleNode("/File/Name").InnerXml;
                    var DateTime = document.SelectSingleNode("/File/DateTime").InnerXml;

                    var InsertResult = DBController.Insert(FileName, Version, DateTime);
                    if (InsertResult == "true")
                    {
                        ViewBag.Message = "Файл успешно загружен, данные добавлены в базу.";
                        Log.Info("Data from file saved to DB");
                    }
                    else
                    {
                        ViewBag.Message = "Не удалось загрузить данные из файла в базу данных. Ошибка: " + InsertResult;
                        Log.Warn("Failed to save data from file to DB. DB answer: " + InsertResult);
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "XML данные повреждены или отправлен неверный файл.";
                    Log.Warn("Probably XML data in file is incorrect.", ex);
                }
            }
            else
                Log.Info("File is null or content length is zero.");
            return View();
        }

        bool CheckFileName(string Name)
        {
            //Имя файла имеет формат «XX_YY_ZZ.xml», где:
            //•	XX – набор русских букв.Количество символов -не более 100;
            //•	YY – набор цифр. Количество символов – либо 1, либо 10, либо от 14 до 20;
            //•	ZZ – любые символы. Количество символов – не более 7.

            // XX = [А-Яа-я]{1,100}
            // YY = [0-9]{1}|[0-9]{10}|[0-9]{14,20}
            // ZZ = .{1,7}
            Log.Debug("Checking file name. File name: " + Name);
            Match match = Regex.Match(Name, @"([А-Яа-я]{1,100})_([0-9]{1}|[0-9]{10}|[0-9]{14,20})_.{1,7}\.xml$",
            RegexOptions.IgnoreCase);
            if (match.Success)
                return true;
            return false;
        }
    }
}