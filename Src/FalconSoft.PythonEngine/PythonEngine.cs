using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Utils;

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

        //public Dictionary<string, PythonResult> GetFormulaResult(string formulaString, Dictionary<string, object> inParams, Dictionary<string, object> outParams)
        //{
        //    if (string.IsNullOrEmpty(formulaString)) return new Dictionary<string, PythonResult>();
        //    var result = new Dictionary<string, PythonResult>();
        //    foreach (var inParam in inParams)
        //    {
        //        PyScope.SetVariable(inParam.Key, inParam.Value);
        //    }
        //    var sSource = PyEngine.CreateScriptSourceFromString(formulaString);

        //    try
        //    {
        //        sSource.Execute(PyScope);
        //        foreach (var outParam in outParams)
        //        {
        //            var res = outParam.Value;
        //            result.Add(outParam.Key, PyScope.TryGetVariable(outParam.Key, out res) ? new PythonResult(res) : new PythonResult(""));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var eo = PyEngine.GetService<ExceptionOperations>();
        //        var error = eo.FormatException(ex);
        //        return new Dictionary<string, PythonResult> { { "", new PythonResult(error) } };
        //    }
        //    return result;
        //}

        public string GetScriptResult(string formulaString,string outparam, Dictionary<string, object> inParams)
        {
            if (string.IsNullOrEmpty(formulaString)) return string.Empty;
            var variables = GetInputVariables(formulaString);
            foreach (var inParam in variables)
            {
                PyScope.SetVariable(inParam, inParams[inParam]);
            }
            var sSource = PyEngine.CreateScriptSourceFromString(formulaString);

            try
            {
                sSource.Execute(PyScope);
                string result;
                PyScope.TryGetVariable(outparam, out result);
                return result;
            }
            catch (Exception ex)
            {
                var eo = PyEngine.GetService<ExceptionOperations>();
                var error = eo.FormatException(ex);
                return string.Empty;
            }
        }

        private IEnumerable<string> GetInputVariables(string formula)
        {
            var regex = new Regex(@"(?<=\[\')(.*?)(?=\'\])");
            return regex.Matches(formula).Select(s => s.ToString()).ToList();
        }
    }
}
