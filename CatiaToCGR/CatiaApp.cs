using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using INFITF;
using MECMOD;
using PARTITF;
using KnowledgewareTypeLib;
using HybridShapeTypeLib;
using ProductStructureTypeLib;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;

namespace CatiaToCGR
{
    class CatiaApp
    {
        const string CATIAapp = "CATIA.Application";
        public INFITF.Application myCATIA { get; set; }


        public string ExportToCgr(string path, string dir)
        {
            string dirName = path.Substring(0,path.LastIndexOf('\\'));
            dirName = dirName.Substring(0, dirName.LastIndexOf('\\'));
            dirName = dirName.Substring(dirName.LastIndexOf('\\') + 1);
            string name = path.Substring(path.LastIndexOf('\\') + 1);
            name = name.Substring(0, name.LastIndexOf('.'));

            ProductDocument prodDoc = null;
            try
            {
                prodDoc = (ProductDocument)myCATIA.Documents.Read(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Path '" + path + "' could not be read.");
                String errType = ex.GetType().Name;
                if (errType == "TimeoutException")
                {
                    //TimeoutException e = ex as TimeoutException;
                    Console.WriteLine("It's the TimeoutException !");
                }
                if (errType == "COMException")
                {
                    COMException COMex = ex as COMException;
                    Console.WriteLine("COMerror " + COMex.ErrorCode.ToString() + ": " + COMex.Message);
                }
                if (prodDoc != null) prodDoc.Close();
                return null;
            }

            string prodDocName = dir + "\\" + dirName + " - " + name;
            if (System.IO.File.Exists(prodDocName + ".cgr"))
                System.IO.File.Delete(prodDocName + ".cgr");
            try
            {
                prodDoc.ExportData(prodDocName + ".CGR", "cgr");
            }
            catch (Exception)
            {
                Console.WriteLine("Path '" + path + "' could not be exported.");
                if (prodDoc != null) prodDoc.Close();
                return null;
            }

            if (prodDoc != null) prodDoc.Close();
            return prodDocName;
        }


        public void SaveCgrToProject(string docName)
        {
            Documents docs = myCATIA.Documents;
            ProductDocument prodDoc = (ProductDocument)docs.Add("Product");
            Product prod = prodDoc.Product;
            Products prods = prod.Products;
            Array arr = Array.CreateInstance(typeof(object), 1);
            arr.SetValue(docName + ".cgr", 0);
            prods.AddComponentsFromFiles(arr, "All");

            string dir = docName.Substring(0, docName.LastIndexOf('\\') + 1);
            string name = docName.Substring(docName.LastIndexOf('\\') + 1);
            docName = dir + RemoveDiacritics(name);
            prodDoc = (ProductDocument)myCATIA.ActiveDocument;
            if (System.IO.File.Exists(docName + ".CATProduct"))
                System.IO.File.Delete(docName + ".CATProduct");
            try
            {
                prodDoc.SaveAs(docName + ".CATProduct");
            }
            catch (Exception)
            {
                Console.WriteLine("CGR document '" + docName + 
                    "' could not be saved as CATProduct.");
            }
            if (prodDoc != null) prodDoc.Close();
        }


        public static string RemoveDiacritics(string str)
        {
            if (str == null) return null;
            var chars =
                from c in str.Normalize(NormalizationForm.FormD).ToCharArray()
                let uc = CharUnicodeInfo.GetUnicodeCategory(c)
                where uc != UnicodeCategory.NonSpacingMark
                select c;

            var cleanStr = new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
            cleanStr = cleanStr.Replace("Đ", "Dj").Replace("đ", "dj");

            return cleanStr;
        }


        public List<Tuple<String, String>> GetCatiaProductParts(string prodPath)
        {
            ProductDocument prodDoc = (ProductDocument)myCATIA.Documents.Read(prodPath);
            Product prod = prodDoc.Product;

            String path = ((Document)prod.ReferenceProduct.Parent).FullName;
            Console.WriteLine(path);

            List<Tuple<String, String>> prodParts = new List<Tuple<String, String>>();
            var prods = prod.Products;
            Console.WriteLine("Nr of product parts := " + prods.Count);
            foreach (Product item in prods)
            {
                path = ((Document)item.ReferenceProduct.Parent).FullName;
                Console.WriteLine(path);
                prodParts.Add(Tuple.Create<string, string>(path, item.get_Name()));
            }
            return prodParts;
        }


        public String[]  GetCatiaFileProps(string path, string docType)
        {
            Product prod;
            if (docType == "CATProduct")
            {
                prod = ((ProductDocument)myCATIA.Documents.Read(path)).Product;
            }
            else
            {
                prod = ((MECMOD.PartDocument)myCATIA.Documents.Read(path)).Product;
            }
            return new String[] 
            {
                prod.get_Name(),
                prod.get_PartNumber()
            };
        }


        public bool GetOrCreateInstance()
        {
            try
            {
                object CATIAinst = Marshal.GetActiveObject(CATIAapp);
                myCATIA = CATIAinst as INFITF.Application;
                Console.WriteLine("CATIA instance activated !");
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
                try
                {
                    Type CATIAType = Type.GetTypeFromProgID(CATIAapp);
                    object CATIAinst = Activator.CreateInstance(CATIAType);
                    myCATIA = CATIAinst as INFITF.Application;
                    Console.WriteLine("CATIA instance created !");
                }
                catch (Exception Ex1)
                {
                    Console.WriteLine("--- CATIA init ERROR:\n" + Ex1.Message);
                    return false;
                }
            }

            myCATIA.Visible = false;
            myCATIA.RefreshDisplay = true;
            myCATIA.DisplayFileAlerts = false;

            return true;
        }


        public void ReadDocs()
        {
            INFITF.Documents docs = myCATIA.Documents;
            Console.WriteLine("Nbr of documents := " + docs.Count.ToString());
            Console.ReadLine();

            if (docs.Count == 0)
            {
                PartDocument partDoc = (PartDocument)docs.Add("Part");
                Part rootPart = partDoc.Part;
            }

            string temp = string.Empty;
            foreach (Document doc in docs)
            {
                temp += doc.FullName + '\n';
            }
            Console.WriteLine(" --- Components --- \n " + temp);
        }
    }
}
