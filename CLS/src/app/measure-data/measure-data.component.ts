import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { MSG_ERROR_PROCESSING } from '../lib/app-constants';
import { Data, MeasureDataIndexListObject, MeasureDataReceiveObject, MeasureDataResponse } from '../_models/measureData';
import { MeasureDataService } from '../_services/measure-data.service';
import { UserService } from '../_services/user.service';
import { MatSidenav } from '@angular/material/sidenav';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { Subscription } from 'rxjs';
import { ProgressBarMode } from '@angular/material/progress-bar';
import { NavigationService } from '../_services/nav.service';
import { HttpClient } from '@angular/common/http';
import { LoggerService } from '../_services/logger.service';
import { Hierarchy, RegionFilter } from '../_models/regionhierarchy';
//import {MaterialModule} from '.../material/material.module';

@Component({
  selector: 'app-measure-data',
  templateUrl: './measure-data.component.html',
  styleUrls: ['./measure-data.component.scss']
})
export class MeasureDataComponent implements OnInit {

  measureDataResponse: MeasureDataResponse | undefined;
  @Output() progressEvent = new EventEmitter<boolean>();

  //Declarations
  title = 'Measure Data';
  showContentPage = true;
  filterDisplay = {
    intervals: true,
    measureTypes: true,
    hierarchy: true      
  };
  dataRange = "";
  data: Data[] = [];
  calendarId? : number;
  day? : string;
  hierarchyId = null;
  measureTypeId =null;
  explanation =null;
  action =null;
  disabledAll = true;
  btnDisabled = false;
  skMeasureData = "";
  allow = false;
  editValue= false;
  showActionButtons = true;
  editBgColor = true;
  filteredPage = null;
  locked: boolean | undefined;
  filtered: MeasureDataReceiveObject = {
    calendarId: 649,
    day: "3/9/2023",
    hierarchyId: 1,
    measureTypeId: 1,
    explanation: 'explanation-value',
    action: 'action-value',
  } 

  // title = "Region Hierarchy"
  dataSource = new MatTableDataSource([] as Data[])
  displayedColumns = ["name", "value", "units", "explanation", "action", "updated"]
  @ViewChild(MatSort) sort!: MatSort
  private userSubscription = new Subscription()
  //disabledAll = false

  //showError = false
  //showContentPage = true
  drawerTitle = "Filter"
  hierarchy: RegionFilter[] = []
  hierarchyLevels!: { name: string, id: number }[]
  model = {
    id: 0,
    active: false,
    name: "",
    level: 0,
    selectedParent: null as number | number[] | null
  } 

  // Error handling within the component
  //errorMsg = "";
  errorMsg: any = ""   
  showError: boolean = true;
  
  rendered = function(){};

  constructor(private measureDataService: MeasureDataService, 
    private router: Router, 
    private _: NavigationService,
    private logger: LoggerService,
    private http: HttpClient/*, 
    private toastr: ToastrService*/) { }


  ngOnInit(): void {
    //this.getMeasData1();
    //this.getMeasureData(this.filtered);
    //this.getData(this.filtered)
    this.getData2(this.filtered)
  }

    saveFilter() {
      this.logger.logInfo("Do your stuff in here")
  }

  applyFilter(event: Event) {
    const filterValue = (event.currentTarget as HTMLInputElement).value
    this.dataSource.filter = filterValue.trim().toLowerCase()
  }

  identity(_: number, item: { id: number }) {
    return item.id
  }

  //================================================================================================
  // Called from FilterCtrl only
  getMeasureData (filtered: any){  
    //this.showError = false;
    this.disabledAll = true;
    this.dataRange = "";  

    this.filteredPage = filtered;      

    this.calendarId = filtered.calendarId;
    this.day = filtered.day;
    this.hierarchyId = filtered.hierarchyId;
    this.measureTypeId = filtered.measureTypeId;
    this.explanation = filtered.explanation;
    this.action = filtered.action;
    
    // Call Server
    this.progress(true);
    this.measureDataService.getMeasureData(filtered).subscribe({
      next: response => {
        this.measureDataResponse = response
        this.progressEvent.emit(false)
      },
      error: error => {
        this.progressEvent.emit(false)
        //this.showError = true
        //this.errorMsg = error.error
      }
    })
  }

  getMeasData(){
    this.measureDataService.getMeasureData1().subscribe(
      (response: any) => {
        console.log("Response : ", response);
        this.data = response;
        this.disabledAll = false;
      },
      error => {
        console.log("Error : ", error);
      }
    );
  }

  getMeasData1() {
    this.measureDataService.getMeasureData1().subscribe({
      next: response => {
        this.measureDataResponse = response
      }
    })
  }

  getData(filtered: any) {
    this.showError = false;
    this.disabledAll = true;
    this.dataRange = "";

    this.filteredPage = filtered;
    this.calendarId = filtered.calendarId;
    this.day = filtered.day;
    this.hierarchyId = filtered.hierarchyId;
    this.measureTypeId = filtered.measureTypeId;
    this.progress(true);
    this.measureDataService.getMeasureData(filtered).subscribe({
      next: response => {
        if (this.itgIsNull(response.error)) {
          this.calendarId = response.calendarId;
          this.data = response.data;
          this.dataRange = response.range;
          this.allow = response.allow;
          this.locked = response.locked;
          this.editValue= response.editValue;
          this.showActionButtons = this.allow && !this.locked;
          this.measureDataResponse = response;
          //this.loadTable();
          this.progress(false);
        } else {
          this.processLocalError(this.title, response.error.message, response.error.id, null, response.error.authError);
        }
        this.disabledAll = false;
      },
      error: error => {
        this.showError = true;
        this.processLocalError(this.title, error.statusText, -99, error.status, null);
      }
    })
  }
  
  getData2(filtered: any) {
    this.showError = false;
    this.disabledAll = true;
    this.dataRange = "";
    this.filteredPage = filtered;
    this.calendarId = filtered.calendarId;
    this.day = filtered.day;
    this.hierarchyId = filtered.hierarchyId;
    this.measureTypeId = filtered.measureTypeId;
    this.progress(true);
    this.http.get('/api/measureData/index?', {
      params: {
        calendarId: filtered.calendarId,
        day: filtered.day,
        hierarchyId: filtered.hierarchyId,
        measureTypeId: filtered.measureTypeId,
        //measureDataId: null,
        //measureValue: null,
        explanation: filtered.explanation,
        action: filtered.action
      }
    }).subscribe((value: any) => {
      if (this.itgIsNull(value.error)) {
        this.calendarId = value.calendarId;
        this.data = value.data;
        this.dataRange = value.range;
        this.allow = value.allow;
        this.locked = value.locked;
        this.editValue= value.editValue;
        this.showActionButtons = this.allow && !this.locked;
        this.measureDataResponse = value;
        this.loadTable(this.showError);
        this.progress(false);
      } else {
        this.processLocalError(this.title, value.error.message, value.error.id, null, value.error.authError);
      }
      this.disabledAll = false;
    }, (err: any) => {
      this.processLocalError(this.title, err.statusText, -99, err.status, null);
    });
    this.loadTable(false);
  }

  getData3(filtered: any) {
    this.userSubscription = this.measureDataService.getMeasureData(filtered).subscribe({
      next: response => {
          this.dataSource = new MatTableDataSource(response.data)
          this.calendarId = response.calendarId;
          this.data = response.data;
          this.dataRange = response.range;
          this.allow = response.allow;
          this.locked = response.locked;
          this.editValue= response.editValue;
          this.showActionButtons = this.allow && !this.locked;
          this.measureDataResponse = response;
          this.dataSource.sort = this.sort
          console.log("Response data : ", response.data);
          // processLocalError here
      },
      error: (err: any) => {
          this.processLocalError(this.title, err.error.message, err.error.id, null, err.error.authError)
      }
  })
  }

  //================================================================================================

  loadTable(showErr: boolean): void {
    this.showError = false;
    try {
      this.btnDisabled = false;
      // Validates data        
      if (this.itgIsNull(this.data) || (this.data.length == 0)) {
        this.btnDisabled = true;
        if (this.itgIsNull(showErr)) {
          this.processLocalError(this.title, "MSG_DATA_NO_FOUND", null, null, null);
        }
      }
      
      this.populateTable();
      return;
  
    } catch (err) {}
  
    // No Success
    this.processLocalError(this.title, MSG_ERROR_PROCESSING, null, null, null);
    return;
  }

  isBoolShow(str: string | boolean): boolean {
    return ((str === "true") || (str === true));
  }

  toggleFilterOpen(): void {
    // toggle filter side nav
  }

  progress(bool: boolean): void {
    // emit progress event
    this.progressEvent.emit(bool);
  }

  // Reloads table from TableCtrl
  populateTable(): void {
    // emit table event
    //$broadcast('mmTableEvent', this.selCalSelected.id);
    //throw new Error("Method not implemented."); 
  }


  closeError(): void {
    this.errorMsg = "";
    this.showError = false;
  }

  processLocalError(title: string, message: string, id: null | number, status: null | number, authError: boolean | null): void {
    this.errorMsg = this.processError(title, message, id, status);
    this.progress(false);
    this.disabledAll = false;
    this.showContentPage = ( authError !== true );
  }

  processError(title: string, message: string, id: number | null, status: number | null): string {
    throw new Error('Method not implemented.');
  }

  // Make a call for filters
  filters: null | any = null;
  getFiltersFromMain(): void {
    this.showError = false;
    this.disabledAll = true;
    this.filters = null;
    this.progress(true);
    // Call Server
    // pages.measureDataFilter.get(
    //   {},
    //   value => {
    //     if (itgIsNull(value.error)) {
    //       this.filters = value;
    //       this.$broadcast('filtersEvent');
    //       this.progress(false);
    //     } else { 
    //       this.processLocalError('Filters', value.error.message, value.error.id, null, value.error.authError)
    //     }
    //     this.disabledAll = false;
    //   }, err => { 
    //     this.processLocalError('Filters', err.statusText, null, err.status, null)
    //   }
    // );
  }

  // -----------------------------------------------------------------------------
  
    // Popover
    dPopover = {
      templateUrl: 'mmPopoverTemplate.html',
      id: '',
      title: '',
      value: '',
      target: '',
      yellow: '',
      expression: '',
      evaluated: '',
      description: '',
      calculated: false
    };
  
    mmPopover(data: any): void {
      this.dPopover.id = data.id;
      this.dPopover.title = data.name;
      this.dPopover.value = this.itgStrNullToEmpty(data.value);
      this.dPopover.target = this.itgStrNullToEmpty(data.target);
      this.dPopover.yellow = this.itgStrNullToEmpty(data.yellow);
      this.dPopover.expression = data.expression;
      this.dPopover.evaluated = data.evaluated;
      this.dPopover.description = data.description;
      this.dPopover.calculated = data.calculated;
    }

  //------------------------------------------------------------------------------------
 
  // Selection Calculated    
  selCalculated = [
    {id:0, name: "Calculated"},
    {id:1, name: "Manual"},
    {id:2, name: "Manual and Calculated"}
  ];
  //selCalSelected = selCalculated[itgSelCal.all];
  //this.selCalSelected = this.selCalculated[itgSelCal.all];
  selCalSelected = this.selCalculated[0];

  onselCalChange(): void {
    this.populateTable();
  }    

  // -----------------------------------------------------------------------------
  // Styles
  // -----------------------------------------------------------------------------
  
  // Measure Value Bg colors
  getBgColor(data: any): string {
    // if ( !this.editBgColor ) return "";   
    // if ( itgIsEmpty(data.value) ) return "";
    // if ( itgIsNull(data.target) && itgIsNull(data.yellow) ) return "";
    if (!this.editBgColor) {
      return "";
    }

    if (!data.value) {
      return "";
    }

    if (!data.target && !data.yellow) {
      return "";
    }

    const red = "bg-danger2";
    const yellow = "bg-warning2";
    const green = "bg-success2";
    let result = red;

    if (!data.target) {
      if (data.value >= data.yellow) {
        return green;
      }
      return result;
    }
    if (!data.yellow) {
      if (data.value >= data.target) {
        return green;
      }
      return result;
    }
    if (data.target >= data.yellow) {
      if (data.value >= data.yellow) {
        result = yellow;
      }
      if (data.value >= data.target) {
        result = green;
      }
    }
    if (data.target < data.yellow) {
      if (data.value <= data.yellow) {
        result = yellow;
      }
      if (data.value <= data.target) {
        result = green;
      }
    }
    return result;
  }

  getBorderColor(targetVal: any, yellowVal: any): void {
    const red = 'border-danger2';
    const yellow = 'border-warning2';
    const green = 'border-success2';
  
    const mVal = document.querySelectorAll('.mVal');
    mVal.forEach(elem => elem.classList.remove(red, yellow, green));
    
    if (this.itgIsEmpty(mVal)) {
      return;
    }
  
    if (this.itgIsNull(targetVal)) {
      if (mVal >= yellowVal) {
        mVal.forEach(elem => elem.classList.add(green));
        return;
      }
      mVal.forEach(elem => elem.classList.add(red));
      return;
    }
  
    if (this.itgIsNull(yellowVal)) {
      if (mVal >= targetVal) {
        mVal.forEach(elem => elem.classList.add(green));
        return;
      }
      mVal.forEach(elem => elem.classList.add(red));
      return;
    }
  
    if (targetVal >= yellowVal) {
      if (mVal >= targetVal) {
        mVal.forEach(elem => elem.classList.add(green));
        return;
      }
      if (mVal >= yellowVal) {
        mVal.forEach(elem => elem.classList.add(yellow));
        return;
      }
    }
    if (targetVal < yellowVal) {
      if (mVal <= targetVal) {
        mVal.forEach(elem => elem.classList.add(green));
        return;
      }
      if (mVal <= yellowVal) {
        mVal.forEach(elem => elem.classList.add(yellow));
        return;
      }
    }
    mVal.forEach(elem => elem.classList.add(red));
  }

          
  // -----------------------------------------------------------------------------
  // Utils
  // -----------------------------------------------------------------------------
  
  itgIsEmpty(value: any): boolean {
    if (!this.itgIsNull(value)) {
      const str = value.toString().trim();
      return str.length === 0;
    }
    return true;
  }
  
  itgIsNull(value: any): boolean {
    return value === undefined || value === null || value !== value;
  }

  itgStrNullToEmpty(str: string): string {
    let ret = str;
    if (str === null || str === "null"){
      ret = "";
    }
    return ret; 
  }

  itgIsNumeric(data: any): boolean {
    return !isNaN(parseFloat(data)) && isFinite(data);
  }
        
  // -----------------------------------------------------------------------------
  // Buttons
  // -----------------------------------------------------------------------------
  
  refresh() {
    //if ( !itgIsNull(filteredPage) ){
    if (this.filteredPage) {
      this.getData(this.filteredPage);
    }
  }
  
  edit(data: any) {
    if (!this.allow || this.locked) { return; }
    
    this.disabledAll = true;
    const id = data.id;
  
    if (this.editValue && !data.calculated) {
      this.editBgColor = false;
      const idA = document.querySelector(`.tdA${id}`);
      // if (idA)
      // {
      //   idA.classList.remove('bg-warning2', 'bg-success2', 'bg-danger2');
      //   idA.innerHTML = '';
      // } 

      let dirVal = 'only-digits';
      /*if (data.unitId === itgUnits.percentage) {
        dirVal = 'zero-to-one';
      }*/
  
      const mVal = this.itgStrNullToEmpty(data.value);
      const mVal2 = `<input type="text" class="form-control mVal mVal${id}"
        value="${mVal}" maxlength="24" [(ngModel)]="mVal"
        (ngModelChange)="getBorderColor(${data.target},${data.yellow})" ${dirVal}>`;
      
      if (idA)
      {
        idA.classList.remove('bg-warning2', 'bg-success2', 'bg-danger2');
        idA.innerHTML = '';
        //idA.appendChild(mVal2);
      } 
    }
  
    const idB = document.querySelector(`.tdB${id}`);
    if(idB)
    {
      idB.innerHTML = '';
      idB.innerHTML = `<textarea class="mExp${id}" rows="2" maxlength="300">${this.itgStrNullToEmpty(data.explanation)}</textarea>`;
    }
    const idC = document.querySelector(`.tdC${id}`);
    if(idC)
    {
      idC.innerHTML = '';
      idC.innerHTML = `<textarea class="mAct${id}" rows="2" maxlength="300">${this.itgStrNullToEmpty(data.action)}</textarea>`;
    }
  
    // document.querySelector(`.edit${id}`).style.display = 'none';
    // document.querySelector(`.btnEdit${id}`).style.display = 'block';
    // document.querySelectorAll('.btnEdit').forEach((btn) => btn.setAttribute('disabled', 'true'));
    // document.querySelector(`.btnEdit${id}`).removeAttribute('disabled');
  }
  
  cancel(data: any) {
    this.disabledAll = false;
    const id = data.id;
  
    if (this.editValue && !data.calculated) {
      if (this.editValue) {
        this.editBgColor = true;
        const idA = document.querySelector(`.tdA${id}`);
        if (idA) {
          idA.classList.add(this.getBgColor(data));
          idA.innerHTML = `<span>${this.itgStrNullToEmpty(data.value)}</span>`;
        }
      }
    }
    const idB = document.querySelector(`.tdB${id}`);
    if(idB){
      idB.innerHTML = `<span>${this.itgStrNullToEmpty(data.explanation)}</span>`;
    }
    const idC = document.querySelector(`.tdC${id}`);
    if (idC){
      idC.innerHTML = `<span>${this.itgStrNullToEmpty(data.action)}</span>`;
    }
    // document.querySelector(`.btnEdit${id}`).style.display = 'none';
    // document.querySelector(`.edit${id}`).style.display = 'block';
  }

  save(data: any): void {
    if (!this.allow || this.locked) {
      return;
    }
  
    this.showError = false;
    this.disabledAll = true;
    let msg = '';
    const id = data.id;
    let mVal = data.value;
  
    if (this.editValue && !data.calculated) {
      const mVal = (<HTMLInputElement>document.querySelector('.mVal' + id)).value;
      if (!this.itgIsEmpty(mVal)) {
        if (!this.itgIsNumeric(mVal)){
          (<HTMLInputElement>document.querySelector('.mVal' + id)).focus();
          const msg = 'Measure Value must be a Number.';
          //return dialog.alert(this.title, msg);
        };
      }
    } 
    
    const mExp = (<HTMLInputElement>document.querySelector('.mExp' + id)).value.trim();
    const mAct = (<HTMLInputElement>document.querySelector('.mAct' + id)).value.trim();
  
    data.explanation = this.itgStrNullToEmpty(data.explanation);
    data.action = this.itgStrNullToEmpty(data.action);
  
    const dataNew = { ...data };
  
    if (this.itgIsEmpty(mVal)) {
      dataNew.value = null;
    } else {
      dataNew.value = Number(mVal);
    }
  
    dataNew.explanation = mExp;
    dataNew.action = mAct;
  
    // if (this.isEqual(data, dataNew)) {
    //   msg =
    //     "There are no changes for <br /> '" +
    //     dataNew.name +
    //     "'.<br /> Unable to Save.";
    //   return dialog.alert(this.title, msg);
    // }
  
    // Call Server - PUT
    this.progress(true);
  
    // this.pages.measureData
    //   .update(
    //     {
    //       calendarId: this.calendarId,
    //       day: null,
    //       hierarchyId: this.hierarchyId,
    //       measureTypeId: this.measureTypeId,
    //       measureDataId: data.id,
    //       measureValue: mVal,
    //       explanation: mExp,
    //       action: mAct,
    //     },
    //     (value) => {
    //       if (itgIsNull(value.error) && value.data.length > 0) {
    //         data.value = value.data[0].value;
    //         data.explanation = value.data[0].explanation;
    //         data.action = value.data[0].action;
    //         data.updated = value.data[0].updated;
    //         //logger.logSuccess('Measure ' + data.name + ' updated.');
    //         this.progress(false);
    //         this.cancel(data);
    //       } else {
    //         this.processLocalError(
    //           this.title,
    //           value.error.message,
    //           value.error.id,
    //           null,
    //           value.error.authError
    //         );
    //       }
    //       this.disabledAll = false;
    //     },
    //     (err: { statusText: string; status: number | null; }) => {
    //       this.processLocalError(this.title, err.statusText, null, err.status, null);
    //     }
    //   );
  
    this.cancel(data);
  }

}