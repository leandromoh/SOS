import { HttpClient } from '@angular/common/http';
import { Component, Inject, OnInit } from '@angular/core';
import { format, parseISO, formatDistanceStrict, formatDuration, intervalToDuration, formatDistance, formatDistanceToNow, sub, subHours } from 'date-fns'
import { compareAsc } from 'date-fns/esm';
import { ActiveInstanceInfo } from '../models/activeinstanceinfo';
import { FunctionalTest } from '../models/functionaltest';
import { HangfireJob } from '../models/hangfirejob';
import { LoadTestMetrics, LoadTestResult } from '../models/loadtestmetrics';
import { LogEntries } from '../models/logentries';
import { MongoDbInfo } from '../models/mongodbinfo';
import { PerformanceData } from '../models/performancedata';
import { ProcessInfo } from '../models/providerinfo';
import { SearchIndexInfo } from '../models/searchindexinfo';
import { TestResults } from '../models/testresults';


function dateFormatter(params) {
  if (params.value) {
    return format(parseISO(params.value), 'yyyy-MM-dd HH:mm:ss');
  }
  else {
    return '';
  }
}

function dateSinceFormatter(params) {
  if (params.value) {
    return 'Running for ' + formatDistanceToNow(parseISO(params.value));
  }
  else {
    return '';
  }
}

@Component({
  selector: 'app-status',
  templateUrl: './status.component.html',
  styleUrls: ['./status.component.scss']
})
export class StatusComponent implements OnInit {
  searchindexinfo: SearchIndexInfo;
  mongodbinfo: MongoDbInfo[];
  statuses = [];
  processInfo: ProcessInfo[];
  loadTestSummary: LoadTestResult[];
  healthStatus: HealthStatus;

  processColumnDefs = [
    {
      field: 'processStatus', sortable: true, filter: true, resizable: true, cellRenderer: function (params) {
        return params.value == "Success" ? '<svg height="20" width="30"><circle cx="10" cy="10" r="10" fill="green" /></svg><span>' + params.value + '</span>' :
          '<svg height="20" width="30"><circle cx="10" cy="10" r="10" fill="red" /></svg><span>' + params.value + '</span>'
      }
    },
    {
      field: 'harvestStatus', sortable: true, filter: true, resizable: true, cellRenderer: function (params) {
        return params.value == "Success" ? '<svg height="20" width="30"><circle cx="10" cy="10" r="10" fill="green" /></svg><span>' + params.value + '</span>' :
          '<svg height="20" width="30"><circle cx="10" cy="10" r="10" fill="red" /></svg><span>' + params.value + '</span>'
      }
    },
    { field: 'dataProviderIdentifier', sortable: true, filter: true, resizable: true },
    { field: 'publicProcessCount', sortable: true, filter: true, resizable: true },
    { field: 'protectedProcessCount', sortable: true, filter: true, resizable: true },
    { field: 'processStart', sortable: true, filter: true, resizable: true, valueFormatter: dateFormatter },
    { field: 'processEnd', sortable: true, filter: true, resizable: true, valueFormatter: dateFormatter },
    { field: 'harvestCount', sortable: true, filter: true, resizable: true },
    { field: 'harvestStart', sortable: true, filter: true, resizable: true, valueFormatter: dateFormatter },
    { field: 'harvestEnd', sortable: true, filter: true, resizable: true, valueFormatter: dateFormatter },
    { field: 'harvestNotes', sortable: false, filter: false, resizable: true },
    {
      field: 'latestIncrementalStatus', sortable: true, filter: true, resizable: true, cellRenderer: function (params) {
        return params.value == "2" ? '<svg height="20" width="30"><circle cx="10" cy="10" r="10" fill="green" /></svg><span>' + params.value + '</span>' :
          '<svg height="20" width="30"><circle cx="10" cy="10" r="10" fill="red" /></svg><span>' + params.value + '</span>'
      }
    },
    { field: 'latestIncrementalPublicCount', sortable: true, filter: true, resizable: true },
    { field: 'latestIncrementalProtectedCount', sortable: true, filter: true, resizable: true },
    { field: 'latestIncrementalStart', sortable: true, filter: true, resizable: true, valueFormatter: dateFormatter },
    { field: 'latestIncrementalEnd', sortable: true, filter: true, resizable: true, valueFormatter: dateFormatter },
  ];

  processRowData = [
  ];

  searchIndexColumnDefs = [
    { field: 'node', sortable: true, filter: true, resizable: true },
    { field: 'percentage', sortable: true, filter: true, resizable: true },
    { field: 'diskUsed', sortable: true, filter: true, resizable: true },
    { field: 'diskTotal', sortable: true, filter: true, resizable: true },
  ];
  mongodbInfoColumnDefs = [
    { field: 'node', sortable: true, filter: true, resizable: true },
    { field: 'percentage', sortable: true, filter: true, resizable: true },
    { field: 'diskUsed', sortable: true, filter: true, resizable: true },
    { field: 'diskTotal', sortable: true, filter: true, resizable: true },
  ];
  processingJobsRowData = [];
  processingJobsColumnDefs = [
    { field: 'invocationData', width: 600, sortable: true, filter: true, resizable: true },
    { field: 'createdAt', sortable: true, filter: true, resizable: true, valueFormatter: dateFormatter },
    { field: 'createdAt', headerName: 'Runtime', sortable: true, filter: true, resizable: true, valueFormatter: dateSinceFormatter }
  ];
  activeInstance: string;
  runningTests: boolean = false;
  completedTests: number = 0;
  failedTests: number = 0;
  totalRuntimeMs: number = 0;
  hostingenvironment: Environment;
  dataComparison: DataCompare[] = [];
  totalDataDifference: number = 0;
  performanceComparison: DataCompare[] = [];
  failedCalls: FailedCalls[] = [];
  sumFailedCalls: number = 0;
  activeInstanceHarvestIsOlderThanOneDay = false;
  logDescription: string;
  constructor(public http: HttpClient, @Inject('BASE_URL') public baseUrl: string) {

  }

  ngOnInit() {
    this.statuses = [];
    this.http.get<ActiveInstanceInfo>(this.baseUrl + 'statusinfo/activeinstance').subscribe(result => {
      this.activeInstance = result.activeInstance.toString();
      this.http.get<ProcessInfo[]>(this.baseUrl + 'statusinfo/process').subscribe(result => {
        this.processInfo = result;
        this.totalDataDifference = 0;
        let active = this.processInfo.find(p => p.id.endsWith(this.activeInstance));
        var activeEndDate = parseISO(active.end);
        var oneDayAgo = subHours(new Date(), 24);
        if (compareAsc(activeEndDate, oneDayAgo) == -1) {
          this.activeInstanceHarvestIsOlderThanOneDay = true;
        }
        else {
          this.activeInstanceHarvestIsOlderThanOneDay = false;
        }
        let inactive = this.processInfo.find(p => !p.id.endsWith(this.activeInstance) && p.id.includes("observation"));
        for (let provider of active.providersInfo) {
          let compare = new DataCompare();
          compare.source = provider.dataProviderIdentifier;
          compare.today = provider.publicProcessCount + provider.protectedProcessCount;

          if (inactive) {
            let inactiveprovider = inactive.providersInfo.find(p => p.dataProviderId == provider.dataProviderId);

            if (inactiveprovider) {
              compare.yesterday = inactiveprovider.publicProcessCount + inactiveprovider.protectedProcessCount;
            } else {
              compare.yesterday = 0;
            }
          } else {
            compare.yesterday = 0;
          }

          this.totalDataDifference += compare.today - compare.yesterday;
          this.dataComparison.push(compare);
        }
      }, error => console.error(error));
    }, error => console.error(error));

    this.http.get<SearchIndexInfo>(this.baseUrl + 'statusinfo/searchindex').subscribe(result => {
      this.searchindexinfo = result;
    }, error => console.error(error));
    this.http.get<LoadTestResult[]>(this.baseUrl + 'performance/loadtestsummary').subscribe(result => {
      this.loadTestSummary = result;
    }, error => console.error(error));
    this.http.get<MongoDbInfo>(this.baseUrl + 'statusinfo/mongoinfo').subscribe(result => {
      this.mongodbinfo = []
      let info = new MongoDbInfo();
      info.diskUsed = result.diskUsed;
      info.diskTotal = result.diskTotal;
      info.node = "Mongo";
      info.percentage = (result.diskUsed / result.diskTotal) * 100;
      info.percentage = Math.floor(info.percentage);
      this.mongodbinfo.push(info);
    }, error => console.error(error));
    this.http.get<HangfireJob[]>(this.baseUrl + 'statusinfo/processing').subscribe(result => {
      this.processingJobsRowData = result;
    }, error => console.error(error));
    this.http.get<FailedCalls[]>(this.baseUrl + 'performance/failed').subscribe(result => {
      this.failedCalls = result;
      this.sumFailedCalls = 0;
      for (var call of this.failedCalls) {
        this.sumFailedCalls += call.count;
      }
    }, error => console.error(error));
    this.http.get<Environment>(this.baseUrl + 'hostingenvironment').subscribe(result => {
      this.hostingenvironment = result;
    }, error => console.error(error));
    this.http.get<LogEntries>(this.baseUrl + "logs/latest?timespan=24h&take=1").subscribe(result => {
      let aggregations = result.aggregations;
      let agg = aggregations.filter(p => p.name === "Log Levels")[0];
      let infoCount = 0;
      let debugCount = 0;
      let errorCount = 0;
      if (agg) {
        if (agg.terms.filter(p => p.name == "Info").length > 0) { infoCount = agg.terms.filter(p => p.name == "Info")[0].docCount; }
        if (agg.terms.filter(p => p.name == "Debug").length > 0) { debugCount = agg.terms.filter(p => p.name == "Debug")[0].docCount; }
        if (agg.terms.filter(p => p.name == "Error").length > 0) { errorCount = agg.terms.filter(p => p.name == "Error")[0].docCount; }
      }
      this.logDescription = "Logs last 24h: Info (" + infoCount + ") Debug (" + debugCount + ") Error (" + errorCount + ")";
    }, error => console.error(error));
    this.runTests();
    this.http.get<PerformanceData>(this.baseUrl + 'performance?timespan=P3D&interval=P1D').subscribe(result => {
      this.performanceComparison = [];
      for (var request of result.requests) {
        if (request.length == 4) {
          let compare = new DataCompare();
          compare.source = request[0].requestName;
          compare.today = request[2].timeTakenMs;
          compare.yesterday = request[1].timeTakenMs;
          this.performanceComparison.push(compare);
        }
      }
    });

   this.http.get<HealthStatus>(this.baseUrl + 'health').subscribe(result => {
   this.healthStatus = result;
   }, error => console.error(error));
  }

  formatDate(param) {
    return format(parseISO(param), 'yyyy-MM-dd HH:mm:ss');
  }
  formatFrom(param) {
    return formatDistanceToNow(parseISO(param))
  }
  getTimeTaken(start, end) {
    var startDate = parseISO(start);
    var endDate = parseISO(end);
    var duration = intervalToDuration({
      start: startDate,
      end: endDate
    })
    return formatDuration(duration);
  }
  getActiveInfo(providerId: string) {
    if (this.isActiveProvider(providerId)) {
      return "(active)";
    }
    if (this.isActiveProvider(providerId)) {
      return "(active)";
    }
    return '';
  }
  isActiveProvider(providerId: string) {
    if (this.activeInstance == "0" && providerId.endsWith("0")) {
      return true;
    }
    if (this.activeInstance == "1" && providerId.endsWith("1")) {
      return true;
    }
    return false;
  }
  isPerformanceRed(today: number, yesterday: number): boolean {
    let percentage = yesterday / today;
    if (percentage <= 0.5) {
      return true;
    }
    return false;
  }
  isPerformanceYellow(today: number, yesterday: number): boolean {
    let percentage = yesterday / today;
    if (percentage < 1.0 && percentage > 0.5) {
      return true;
    }
    else {
      return false;
    }
  }
  isPerformanceGreen(today: number, yesterday: number): boolean {
    if (today <= yesterday) {
      return true;
    }
    else {
      return false;
    }
  }
  getYesterdayDate() {
    return format(subHours(new Date(), 24), 'MMM dd')
  }
  getYesterYesterdayDate() {
    return format(subHours(new Date(), 48), 'MMM dd')
  }
  gaugeColorFunction(value: number): string {
    if (value < 75) {
      return "green";
    }
    else if (value > 75 && value < 90) {
      return "blue";
    }
    else {
      return "red";
    }
  }
  gaugeStatusColorFunction(value: number): string {
    if (value > 75) {
      return "green";
    }
    if (value > 40) {
      return "blue";
    }
   
    return "red";
  }
  gaugeLabelFunction(value: number): string {
    return value + '%';
  }

  private runTests() {
    this.runningTests = true;
    this.completedTests = 0;
    this.failedTests = 0;
    this.totalRuntimeMs = 0;
    this.http.get<FunctionalTest[]>(this.baseUrl + 'tests').subscribe(result => {
      for (let test of result) {
        test.currentStatus = "Unknown";
      }
      let testsRemaining = result.length;
      for (let test of result) {
        this.http.get<TestResults>('tests/' + test.route).subscribe(result => {
          if (result) {
            this.totalRuntimeMs += result.timeTakenMs;
            for (let message of result.results) {
              if (message.status == "Succeeded") { this.completedTests++; }
              if (message.status == "Failed") { this.failedTests++; }
            }
          }
          testsRemaining--;
          if (testsRemaining == 0) {
            this.runningTests = false;
          }
        }, error => {
          testsRemaining--;
          if (testsRemaining == 0) {
            this.runningTests = false;
          }
          this.failedTests++;
        });

      }

    }, error => console.error(error));
  }
}
class DataCompare {
  source: string;
  today: number;
  yesterday: number;
}
class FailedCalls {
  name: string;
  count: number;
}
class HealthStatus {
  status: string;
  duration: object;
  entries: Array<HealthEntry>;
}
class HealthEntry {
  key: string;
  status: string;
  duration: object;
  tags: Array<string>;
}
