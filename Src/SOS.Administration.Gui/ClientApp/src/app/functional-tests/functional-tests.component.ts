import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Message } from '@angular/compiler/src/i18n/i18n_ast';
import { Component, Inject, OnInit } from '@angular/core';
import { format, parseISO } from 'date-fns';
import { Observable } from 'rxjs';
import { FunctionalTest } from '../models/functionaltest';
import { TestResults } from '../models/testresults';

@Component({
  selector: 'app-functional-tests',
  templateUrl: './functional-tests.component.html',
  styleUrls: ['./functional-tests.component.scss']
})
export class FunctionalTestsComponent implements OnInit {
  functionalTests: FunctionalTest[] = [];
  testResults: TestResults[] = [];
  messageList: TestMessage[] = [];
  http: HttpClient;
  loadingData: boolean = false;
  constructor(http: HttpClient, @Inject('BASE_URL') public baseUrl: string) {
    this.http = http;   
  }
  ngOnInit() {
    this.fetchTests();    
  }  
  private fetchTests() {
    this.loadingData = true;
    this.http.get<FunctionalTest[]>(this.baseUrl + 'tests').subscribe(result => {
      this.functionalTests = result;
      for (let test of this.functionalTests) {
        test.currentStatus = "Unknown";
      }
      this.loadingData = false;
      this.runTests();
    }, error => console.error(error));  
  }
  public runTests() {
    this.messageList = [];
    for (let test of this.functionalTests) {
      test.currentStatus = "Unknown";
      test.errorMessages = "";
    }
    this.testResults = [];
    for (let test of this.functionalTests) {
      this.messageList.push({ timestamp: new Date(), message: "Running test:'" + test.description + "'", type: "Info" });
      this.http.get<TestResults>('tests/' + test.route).subscribe(result => {
        this.testResults.push(result);
        if (result) {
          this.setTestStatus(test, result);
          for (let message of result.results) {
            this.messageList.push({ timestamp:new Date(), message: test.description + ': ' + message.result, type: message.status });
          }
        }
      }, error => this.messageList.push({ timestamp:new Date(),message: error.message, type: "Failed" }));
    }
  }
  setTestStatus(test: FunctionalTest, result: TestResults) {
    test.timeTakenMs = result.timeTakenMs;
    let failed: boolean = false;
    for (let res of result.results) {
      if (res.status != "Succeeded") {
        failed = true;
        test.errorMessages += res.result + ", ";       
      }
    }
    if (failed) {
      test.currentStatus = "Failed";
    }
    else {
      test.currentStatus = "Succeeded";
    }
  }
  getTotalTime() {
    let totalTime: number = 0;
    for (let result of this.testResults) {
      totalTime += result.timeTakenMs;
    }
    return totalTime;
  }
  getGroups() {
    let groups = [];
    for (let test of this.functionalTests) {
      if (!groups.includes(test.group)) {
        groups.push(test.group)
      }
    }
    return groups;
  }
  getTests(group) {    
    return this.functionalTests.filter(p => p.group == group);
  }
  getTestClass(type) {   
    if (type == "Succeeded") { return "list-group-item list-group-item-success"; }
    if (type == "Failed") { return "list-group-item list-group-item-danger"; }
    return "list-group-item"; 
  }
  getMessageClass(type) {
    if (type == "Succeeded") { return "list-group-item-success"; }
    if (type == "Failed") { return "list-group-item-danger"; }
    return "";
  }
  getCompletedTestCount() {
    let count:number = 0;
    for (let result of this.testResults) {
      for (let res of result.results) {
        if (res.status == "Succeeded") { count++;}
      }
    }
    return count;
  }
  getFailedTestCount() {
    let count: number = 0;
    for (let result of this.testResults) {
      for (let res of result.results) {
        if (res.status == "Failed") { count++; }
      }
    }
    return count;
  }
  formatDate(params) {
  if (params) {
    return format(parseISO(params), 'HH:mm:ss');
  }
  else {
    return '';
  }
}
}
class TestMessage {
  type: string;
  message: string;
  timestamp: Date;
}
