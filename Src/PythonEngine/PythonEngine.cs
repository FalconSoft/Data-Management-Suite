using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace FalconSoft.PythonEngine
{
    public class PythonEngine
    {
        private static readonly ScriptEngine PyEngine = Python.CreateEngine();
        private static readonly ScriptScope PyScope = PyEngine.CreateScope();

        public string GetScriptResult(string formulaString, Dictionary<string, object> row)
        {
            if (string.IsNullOrEmpty(formulaString)) return string.Empty;
            PyScope.SetVariable("row", row);
            var sSource = formulaString.IndexOf("Result", StringComparison.OrdinalIgnoreCase) != -1 ? PyEngine.CreateScriptSourceFromString(formulaString) : PyEngine.CreateScriptSourceFromString("Result=" + formulaString);
            try
            {
                sSource.Execute(PyScope);
                object res;
                PyScope.TryGetVariable("Result", out res);
                return res.ToString();
            }
            catch (Exception ex)
            {
                var eo = PyEngine.GetService<ExceptionOperations>();
                var error = eo.FormatException(ex);
                return "Error";
            }
        }

        public Dictionary<string, PythonResult> GetFormulaResult(string formulaString, Dictionary<string, object> inParams, Dictionary<string, object> outParams)
        {
            if (string.IsNullOrEmpty(formulaString)) return new Dictionary<string, PythonResult>();
            var result = new Dictionary<string, PythonResult>();
            foreach (var inParam in inParams)
            {
                PyScope.SetVariable(inParam.Key, inParam.Value);
            }
            var sSource = PyEngine.CreateScriptSourceFromString(formulaString);

            try
            {
                sSource.Execute(PyScope);
                foreach (var outParam in outParams)
                {
                    var res = outParam.Value;
                    result.Add(outParam.Key, PyScope.TryGetVariable(outParam.Key, out res) ? new PythonResult(res) : new PythonResult(""));
                }
            }
            catch (Exception ex)
            {
                var eo = PyEngine.GetService<ExceptionOperations>();
                var error = eo.FormatException(ex);
                return new Dictionary<string, PythonResult> { { "", new PythonResult(error) } };
            }
            return result;
        }
    }

    public class PythonResult
    {
        public PythonResult(object value)
        {
            ResultValue = value;
        }
        public PythonResult(string error)
        {
            Error = error;
        }
        public PythonResult(object value, string error)
        {
            ResultValue = value;
            Error = error;
        }
        public object ResultValue { get; set; }
        public string Error { get; set; }
        public bool HasError
        {
            get { return !string.IsNullOrEmpty(Error); }
        }

        public override string ToString()
        {
            return HasError ? Error : ResultValue.ToString();
        }
    }
}
