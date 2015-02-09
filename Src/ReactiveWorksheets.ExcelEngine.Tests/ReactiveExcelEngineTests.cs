using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.ExcelAddIn.ReactiveExcel;
using NUnit.Framework;
using ReactiveWorksheets.Server.Tests;

namespace ReactiveWorksheets.ExcelEngine.Tests
{
    [TestFixture]
    public class ReactiveExcelEngineTests:JsonObjects
    {

        [Test]
        public void CorrectInputDataTest()
        {
            var serverMsg = new Subject<RecordChangedParam[]>();
            var engine = new MockExcelEngine(serverMsg);
            var rFunctions = new MockReactiveFunctions(engine);
            var result1 = (string)rFunctions.RDP(string.Empty, "10788", "CustomerID");
            Assert.AreEqual("Invalid Input Parameters",result1);
            var result2 = (string)rFunctions.RDP(@"test\Orders", string.Empty, "CustomerID");
            Assert.AreEqual("Invalid Input Parameters", result2);
            var result3 = (string)rFunctions.RDP(@"test\Orders", "10788", string.Empty);
            Assert.AreEqual("Invalid Input Parameters", result3); 
        }

        [Test]
        public void RegisterSourceTest()
        {
            var serverMsg = new Subject<RecordChangedParam[]>();
            var engine = new MockExcelEngine(serverMsg);
            var rFunctions = new MockReactiveFunctions(engine);
            var obs = rFunctions.RDP(@"Test\Orders", "10788", "CustomerID");
            Assert.AreEqual("#Loading!", obs.ToString());
            Assert.AreEqual("Invalid Urn", engine.ResultValue);
            engine.AddDsForRegister(OrdersDataSourceInfoJsonClean, @"OrderID	CustomerID	EmployeeID	OrderDate	CustomerCompany	CustomerContact	CustomerContactTitle
                                                            10788	TRAIH	1	22/12/1997	Trail's Head Gourmet Provisioners	Helvetius Nagy	Sales Associate");
            Assert.AreEqual(engine.LocalDb.Count,1);
            Assert.AreEqual(engine.LocalDb.First().Key, @"Test\Orders");
            Assert.AreEqual(engine.LocalDb.First().Value.Count, 1);
            Assert.AreEqual(engine.LocalDb.First().Value.First().Key, "|10788");
            Assert.AreEqual(engine.LocalDb.First().Value.First().Value.Count, 7);
        }

        [Test]
        public void RegisterSubjectTest()
        {
            var serverMsg = new Subject<RecordChangedParam[]>();
            var engine = new MockExcelEngine(serverMsg);
            var rFunctions = new MockReactiveFunctions(engine);
            engine.AddDsForRegister(OrdersDataSourceInfoJsonClean, @"OrderID	CustomerID	EmployeeID	OrderDate	CustomerCompany	CustomerContact	CustomerContactTitle
                                                            10788	TRAIH	1	22/12/1997	Trail's Head Gourmet Provisioners	Helvetius Nagy	Sales Associate");
            var obs = rFunctions.RDP(@"Test\Orders", "10788", "CustomerID");
            Assert.AreEqual("#Loading!", obs.ToString());
            Assert.AreEqual("TRAIH", engine.ResultValue);
        }


        [Test]
        public void ExcelEngineFunctionalityTest()
        {
            var serverMsg = new Subject<RecordChangedParam[]>();
            var engine = new MockExcelEngine(serverMsg);
            var rFunctions = new MockReactiveFunctions(engine);
            engine.AddDsForRegister(OrdersDataSourceInfoJsonClean, @"OrderID	CustomerID	EmployeeID	OrderDate	CustomerCompany	CustomerContact	CustomerContactTitle
                                                            10788	TRAIH	1	22/12/1997	Trail's Head Gourmet Provisioners	Helvetius Nagy	Sales Associate");
            engine.IsRdpSubcribed = false;
            var obs = rFunctions.RDP(@"Test\Orders", "10788", "CustomerID");
            Assert.AreEqual("TRAIH", engine.ResultValue);
            var ds = MockRepository.GetDataSourceFromJSON(OrdersDataSourceInfoJsonClean);
            var data = RecordHelpers.TsvToDictionary(ds,
                @"OrderID	CustomerID	EmployeeID	OrderDate	CustomerCompany	CustomerContact	CustomerContactTitle
                                                            10788	FalconSoft	1	22/12/1997	Trail's Head Gourmet Provisioners	Helvetius Nagy	Sales Associate").First();
            //on Update
            var rcp = new RecordChangedParam()
            {
                ChangeSource = @"Test\Orders",
                ChangedAction = RecordChangedAction.AddedOrUpdated,
                IgnoreWorksheet = null,
                UserToken = "1",
                ProviderString = @"Test\Orders",
                OriginalRecordKey = "|10788",
                RecordKey = "|10788",
                ChangedPropertyNames = new[] { "CustomerID" },
                RecordValues = data
            };
            serverMsg.OnNext(new[] { rcp });
            Assert.AreEqual(engine.LocalDb[@"Test\Orders"]["|10788"]["CustomerID"], "FalconSoft");
            //on Add
            var data2 = RecordHelpers.TsvToDictionary(ds,
               @"OrderID	CustomerID	EmployeeID	OrderDate	CustomerCompany	CustomerContact	CustomerContactTitle
                                                            1488	Hello	2	22/12/2014	OK	One	Two").First();
            var rcp2 = new RecordChangedParam()
            {
                ChangeSource = @"Test\Orders",
                ChangedAction = RecordChangedAction.AddedOrUpdated,
                IgnoreWorksheet = null,
                UserToken = "1",
                ProviderString = @"Test\Orders",
                OriginalRecordKey = "|1488",
                RecordKey = "|1488",
                ChangedPropertyNames = data2.Keys.ToArray(),
                RecordValues = data2
            };
            serverMsg.OnNext(new[] { rcp2 });
            Assert.IsTrue(engine.LocalDb[@"Test\Orders"].ContainsKey("|1488"));
            Assert.AreEqual(engine.LocalDb[@"Test\Orders"]["|1488"]["OrderID"].ToString(), "1488");
            Assert.AreEqual(engine.LocalDb[@"Test\Orders"]["|1488"]["CustomerID"].ToString(), "Hello");
            Assert.AreEqual(engine.LocalDb[@"Test\Orders"]["|1488"]["EmployeeID"].ToString(), "2");
            Assert.AreEqual(engine.LocalDb[@"Test\Orders"]["|1488"]["CustomerCompany"].ToString(), "OK");
            Assert.AreEqual(engine.LocalDb[@"Test\Orders"]["|1488"]["CustomerContact"].ToString(), "One");
            Assert.AreEqual(engine.LocalDb[@"Test\Orders"]["|1488"]["CustomerContactTitle"].ToString(), "Two");
            //on Remove
            var rcp3 = new RecordChangedParam()
            {
                ChangeSource = @"Test\Orders",
                ChangedAction = RecordChangedAction.Removed,
                IgnoreWorksheet = null,
                UserToken = "1",
                ProviderString = @"Test\Orders",
                OriginalRecordKey = "|1488",
                RecordKey = "|1488",
                ChangedPropertyNames = null,
                RecordValues = null
            };
            serverMsg.OnNext(new[] { rcp3 });
            Assert.IsTrue(!engine.LocalDb[@"Test\Orders"].ContainsKey("|1488"));
            
        }

    }
}
