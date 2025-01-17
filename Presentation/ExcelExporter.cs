﻿using Logging;
using System.IO;
using OfficeOpenXml;
using System.Diagnostics;
using System.Reflection;
using Logic;
using UI_Interfaces;


namespace Presentation
{
    public class ExcelExporter
    {
        private Logger _logger;
        private enum SensitivityLabel
        {
            General,
            Confidential,
            HighlyConfidential
        }
        private string directory;
        public ExcelExporter(Logger logger)
        {
            _logger = logger;
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Saber Tool Plus", "TempData");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

        }

        public void CreateProjectExcelSheet(List<iProject_Wire> wires, List<IProject_Component> components, string fileName, List<Profile> profiles)
        {
            try
            {
                // Start the stopwatch
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // Close any existing instances of "ExtractedData.xlsx"
                foreach (var process in Process.GetProcessesByName("EXCEL"))
                {
                    if (process.MainWindowTitle.Contains($"{fileName}.xlsx"))
                    {
                        process.Kill();
                        process.WaitForExit(); // Wait for the process to exit before continuing
                    }
                }

                using (var package = new ExcelPackage())
                {
                    // Add a worksheet for Wires
                    var wireWorksheet = package.Workbook.Worksheets.Add("Project_Wires");

                    // Write column headers for wires
                    WriteHeaders(wireWorksheet, profiles[0]);

                    // Write wire data
                    WriteDataToSheet(wireWorksheet, wires, profiles[0]);
                    
                    AddAutoFilterButtons(wireWorksheet);

                    // Add a worksheet for Components
                    var componentWorksheet = package.Workbook.Worksheets.Add("Project_Components");

                    //Write column headers for components
                    WriteHeaders(componentWorksheet, profiles[1]);

                    // Write component data
                    WriteDataToSheet(componentWorksheet, components, profiles[1]);
                    AddAutoFilterButtons(componentWorksheet);

                    // Save the Excel package to a file
                    package.SaveAs(new FileInfo(Path.Combine(directory, $"{fileName}.xlsx")));

                    // Get the full path to the Excel file
                    string filePath = Path.Combine(directory, $"{fileName}.xlsx");

                    // Open the Excel file using its default application
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = true; //required by .NET core 6 and higher
                    p.StartInfo.FileName = filePath;
                    p.Start();
                }

                // Stop the stopwatch
                stopwatch.Stop();

                _logger.Log($"Project Excel file opened successfully. Time elapsed: {stopwatch.Elapsed.TotalSeconds}s");
            }
            catch (Exception ex)
            {
                _logger.Log(ex.Message);
            }
        }

        public void CreateExcelSheet(List<iConverted_Wire> wires, List<iConverted_Component> components, string fileName, List<Profile> profiles, List<DSI_Tube> tubes)
        {
            try
            {
                // Start the stopwatch
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // Close any existing instances of "ExtractedData.xlsx"
                foreach (var process in Process.GetProcessesByName("EXCEL"))
                {
                    if (process.MainWindowTitle.Contains($"{fileName}.xlsx"))
                    {
                        process.Kill();
                        process.WaitForExit(); // Wait for the process to exit before continuing
                    }
                }

                using (var package = new ExcelPackage())
                {
                    // Add a worksheet for Wires
                    var wireWorksheet = package.Workbook.Worksheets.Add("Wires");


                    // Write column headers for wires
                    WriteHeaders(wireWorksheet, profiles[0]);

                    // Write wire data
                    WriteDataToSheet(wireWorksheet, wires, profiles[0]);
                    //wireWorksheet.Cells[wireWorksheet.Dimension.Address].AutoFitColumns();
                    AddAutoFilterButtons(wireWorksheet);

                    // Add a worksheet for Components
                    var componentWorksheet = package.Workbook.Worksheets.Add("Components");

                    // Write column headers for components
                    WriteHeaders(componentWorksheet, profiles[1]);

                    // Write component data
                    WriteDataToSheet(componentWorksheet, components, profiles[1]);
                    //componentWorksheet.Cells[componentWorksheet.Dimension.Address].AutoFitColumns();
                    AddAutoFilterButtons(componentWorksheet);

                    var tubeWorksheet = package.Workbook.Worksheets.Add("DSI_Tubing");

                    WriteHeaders_alt(tubeWorksheet, tubes);
                    WriteDataToSheet_alt(tubeWorksheet, tubes);
                    AddAutoFilterButtons(tubeWorksheet);

                    CreateALL_PE_sheet(wires, package);

                    // Save the Excel package to a file
                    package.SaveAs(new FileInfo(Path.Combine(directory, $"{fileName}.xlsx")));

                    // Get the full path to the Excel file
                    string filePath = Path.Combine(directory, $"{fileName}.xlsx");

                    // Open the Excel file using its default application
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = true; //required by .NET core 6 and higher
                    p.StartInfo.FileName = filePath;
                    p.Start();
                }

                // Stop the stopwatch
                stopwatch.Stop();

                _logger.Log($"Excel file created successfully. Time elapsed: {stopwatch.Elapsed.TotalSeconds}s");
            }
            catch (Exception ex)
            {
                _logger.Log(ex.Message);
            }
        }

        private static void AddAutoFilterButtons(ExcelWorksheet worksheet)
        {
            // Assuming headers are in the first row (row 1)
            worksheet.Cells["A1:" + worksheet.Dimension.End.Address].AutoFilter = true;
        }

        private static void WriteHeaders(ExcelWorksheet worksheet, Profile profile)
        {
            if (profile == null)
            {
                // Handle the case where the list is empty
                return;
            }

            PropertyInfo[] properties = typeof(Profile).GetProperties();

            // Get the parameters of the profile
            List<string> parameters = profile.Parameters;

            // Write the parameters as headers
            for (int i = 0; i < parameters.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = parameters[i];
            }

            // Freeze the top row (headers)
            worksheet.View.FreezePanes(2, 1);
        }

        private static void WriteHeaders_alt<T>(ExcelWorksheet worksheet, List<T> objects)
        {
            if (objects.Count == 0)
            {
                // Handle the case where the list is empty
                return;
            }

            PropertyInfo[] properties = objects[0].GetType().GetProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                string header = $"{properties[i].Name}";
                worksheet.Cells[1, i + 1].Value = header;
            }
            worksheet.View.FreezePanes(2, worksheet.Dimension.End.Column + 1);
        }


        private static int GetMaxLength<T>(List<T> objects, PropertyInfo property)
        {
            // Assuming the property is a string type, you may want to modify this logic
            // based on the actual data type of the property.
            int maxLength = property.Name.Length;

            foreach (var obj in objects)
            {
                object value = property.GetValue(obj);
                if (value != null)
                {
                    int length = value.ToString().Length;
                    if (length > maxLength)
                    {
                        maxLength = length;
                    }
                }
            }

            return maxLength;
        }

        private static void WriteDataToSheet<T>(ExcelWorksheet worksheet, List<T> objects, Profile profile)
        {
            if (objects.Count == 0 || profile == null)
            {
                // Handle the case where the list is empty or profile is null
                return;
            }

            // Get the properties to write based on the parameters in the profile
            List<PropertyInfo> propertiesToWrite = new List<PropertyInfo>();
            foreach (string parameter in profile.Parameters)
            {
                PropertyInfo property = typeof(T).GetProperty(parameter);
                if (property != null)
                {
                    propertiesToWrite.Add(property);
                }
            }

            for (int row = 0; row < objects.Count; row++)
            {
                for (int col = 0; col < propertiesToWrite.Count; col++)
                {
                    var propertyValue = propertiesToWrite[col].GetValue(objects[row]);
                    worksheet.Cells[row + 2, col + 1].Value = propertyValue;
                }
            }
        }

        private static void WriteDataToSheet_alt<T>(ExcelWorksheet worksheet, List<T> objects)
        {
            if (objects.Count == 0)
            {
                // Handle the case where the list is empty
                return;
            }

            PropertyInfo[] properties = typeof(T).GetProperties();

            for (int row = 0; row < objects.Count; row++)
            {
                for (int col = 0; col < properties.Length; col++)
                {
                    var propertyValue = properties[col].GetValue(objects[row]);
                    worksheet.Cells[row + 2, col + 1].Value = propertyValue;
                }
            }
        }

        private static object GetCustomPropertyValue(ExcelPackage package, string propertyName)
        {
            // Get the custom properties XML
            var customPropertiesXml = package.Workbook.Properties.CustomPropertiesXml;

            // Check if the custom properties XML is not null
            if (customPropertiesXml != null)
            {
                // Check if the property exists
                var propertyNode = customPropertiesXml.SelectSingleNode($"/Properties/AppProperties[@name='{propertyName}']");
                if (propertyNode != null)
                {
                    // Return the property value
                    return propertyNode.InnerText;
                }
            }

            // Return null if the property is not found
            return null;
        }

        public void CreateALL_PE_sheet(List<iConverted_Wire> wires, ExcelPackage excelPackege)
        {
            //Prepare ALL_PE profile
            List<string> ALL_PE_Strings = new List<string>();
            string[] stringsToAdd = { "Connector_1", "Port_1", "Wire", "Wire_connection", "Diameter", "Color", "Type", "Code_no", "Length", "Term_1", "Seal_1", "Variant", "Bundle" };
            ALL_PE_Strings.AddRange(stringsToAdd);

            Profile ALL_PE_Profile = new Profile("ALL_PE", ALL_PE_Strings, Data_Interfaces.ProfileType.User);

            int originalCount = wires.Count;
            for (int i = 0; i < originalCount; i++)
            {
                Converted_Wire newWire = new Converted_Wire(
                    wires[i].Wire,
                    wires[i].Diameter,
                    wires[i].Color,
                    wires[i].Type,
                    wires[i].Code_no, // Assuming part_no corresponds to the Code_no property
                    wires[i].Length,
                    wires[i].Connector_2,
                    wires[i].Port_2,
                    wires[i].Term_2,
                    wires[i].Seal_2,
                    wires[i].Wire_connection,
                    wires[i].Term_1,
                    wires[i].Seal_1,
                    wires[i].Connector_1,
                    wires[i].Port_1,
                    wires[i].Variant,
                    wires[i].Bundle,
                    wires[i].Loc_1,
                    wires[i].Loc_2
                    );

                string tempConnector = wires[i].Connector_1;
                string tempTerm = wires[i].Term_1;
                string tempSeal = wires[i].Seal_1;
                string tempPort = wires[i].Port_1;

                newWire.Connector_1 = wires[i].Connector_2;
                newWire.Term_1 = wires[i].Term_2;
                newWire.Seal_1 = wires[i].Seal_2;
                newWire.Port_1 = wires[i].Port_2;

                newWire.Connector_2 = tempConnector;
                newWire.Term_2 = tempTerm;
                newWire.Seal_2 = tempSeal;
                newWire.Port_2 = tempPort;

                wires.Add(newWire);
            }

            var wireWorksheet = excelPackege.Workbook.Worksheets.Add("ALL_PE");

            var sortedWires = wires.OrderBy(wire => wire.Connector_1)
                               .ThenBy(wire => int.TryParse(wire.Port_1, out int port) ? port : int.MaxValue)
                               .ToList();


            // Write column headers for wires
            WriteHeaders(wireWorksheet, ALL_PE_Profile);

            // Write wire data
            WriteDataToSheet(wireWorksheet, sortedWires, ALL_PE_Profile);
            
            
            AddAutoFilterButtons(wireWorksheet);
        }
    }
}
