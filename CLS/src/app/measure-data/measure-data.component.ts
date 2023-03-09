import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { MSG_ERROR_PROCESSING } from '../app-constants';
import { MeasureDataService } from '../_services/measure-data.service';
import { UserService } from '../_services/user.service';

@Component({
  selector: 'app-measure-data',
  templateUrl: './measure-data.component.html',
  styleUrls: ['./measure-data.component.css']
})
export class MeasureDataComponent implements OnInit {


  //Declarations
  title = 'Measure Data';
  showContentPage = true;
  filterDisplay = {
    intervals: true,
    measureTypes: true,
    hierarchy: true      
  };
  dataRange = "";
  data = [];
  //rendered = function(){};
  calendarId = null;
  day = null;
  hierarchyId = null;
  measureTypeId =null;
  disabledAll = true;
  btnDisabled = false;
  skMeasureData = "";
  allow = false;
  editValue= false;
  showActionButtons = false;
  editBgColor = true;
  filteredPage = null;
  locked: boolean | undefined;

  constructor(private _measureDataService: MeasureDataService, private router: Router/*, private toastr: ToastrService*/) { }

  ngOnInit(): void {
  }


    
  isBoolShow(str: string | boolean){
    return ((str == "true") || (str == true));
  }
      
  toggleFilterOpen() {
    //$mdSidenav('left').toggle();
    
  };

  progress ( inprog: boolean){
    //$broadcast('ProgressEvent', bool);
  }
    
  // Reloads table from TableCtrl
  populateTable(){
    //$broadcast('mmTableEvent', selCalSelected.id);
    throw new Error("Method not implemented."); 
  }

  // Error handling within the component          
  errorMsg = "";
  showError = false;

  closeError (){
    this.errorMsg = "";
    this.showError = false;
  }

  processLocalError (title: any, message: any, id: any, status: any, authError: any){
    //this.errorMsg = processError(title, message, id, status);----------------------------------------------------------------------------------------------------
    this.progress(false);
    this.showError = true;
    this.disabledAll = false;
    this.showContentPage = ( authError != true ); 
  }
    
  // Make a call for filters
  filters = null;
  getFiltersFromMain(){
    
    this.showError = false;
    this.disabledAll = true;
    this.filters = null;
    this.progress(true);
    // Call Server
    // this.pages.measureDataFilter.get(
    // //pages.general('/jsons/new/pages/filters.json').get(
    //   {},
    //   function (value) {
    //     if (itgIsNull(value.error)) {
    //       filters = value;
    //       $broadcast('filtersEvent');
    //       progress(false);
    //     }
    //     else { 
    //       processLocalError('Filters', value.error.message, value.error.id, null, value.error.authError)
    //     }
    //     disabledAll = false;
    //   }, function (err) { 
    //     processLocalError('Filters', err.statusText, null, err.status, null) }
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
  
  mmPopover (data:any){    
    this.dPopover.id = data.id;
    this.dPopover.title = data.name;
    // this.dPopover.value = itgStrNullToEmpty(data.value);
    // this.dPopover.target = itgStrNullToEmpty(data.target);
    // this.dPopover.yellow = itgStrNullToEmpty(data.yellow);
    this.dPopover.expression = data.expression;
    this.dPopover.evaluated = data.evaluated;
    this.dPopover.description = data.description;
    this.dPopover.calculated = data.calculated;
  };
  
  // Selection Calculated    
  selCalculated = [
    {id:0, name: "Calculated"},
    {id:1, name: "Manual"},
    {id:2, name: "Manual and Calculated"}
  ];
  //selCalSelected = selCalculated[itgSelCal.all];
  onselCalChange (){
    this.populateTable(); 
  }     

  // Measure Value Bg colors
  getBgColor(data:any){
          
    if ( !this.editBgColor ) return "";   
    // if ( itgIsEmpty(data.value) ) return "";
    // if ( itgIsNull(data.target) && itgIsNull(data.yellow) ) return "";
    
    var red = "bg-danger2";
    var yellow = "bg-warning2";
    var green = "bg-success2";
    var result = red; 
          
    // if ( itgIsNull(data.target) ) {
    //   if ( data.value >= data.yellow ) return green;
    //   return result;
    // }
    
    // if ( itgIsNull(data.yellow) ){
    //   if ( data.value >= data.target ) return green;
    //   return result;
    // }
    
    if ( data.target >= data.yellow ){
      if ( data.value >= data.yellow ){
        result = yellow;
      }
      if ( data.value >= data.target ){
        result = green;
      }
    }
      
    if ( data.target < data.yellow){
      if ( data.value <= data.yellow ){
        result = yellow;
      } 
      if ( data.value <= data.target ){
        result = green;
      }
    }
    
    return result;
  }
    
  // getBorderColor = function(targetVal, yellowVal){
      
  //   var red = "border-danger2";
  //   var yellow = "border-warning2";
  //   var green = "border-success2";

  //   var mVal = $(".mVal");
  //   mVal.removeClass(red).removeClass(yellow).removeClass(green);
  //   if ( itgIsEmpty(mVal) ) return;
          
  //   if ( itgIsNull(targetVal) ) {
  //     if ( mVal >= yellowVal ){
  //         mVal.addClass(green);
  //         return;
  //     }
  //     mVal.addClass(red);
  //     return;
  //   }
    
  //   if ( itgIsNull(yellowVal) ){
  //     if ( mVal >= targetVal ) {
  //       mVal.addClass(green);
  //       return;
  //     }
  //     mVal.addClass(red);
  //     return;
  //   }
    
  //   if ( targetVal >= yellowVal ){
  //     if ( mVal >= targetVal ){
  //       mVal.addClass(green);
  //       return;
  //     } 
  //     if ( mVal >= yellowVal ){
  //       mVal.addClass(yellow);
  //       return;
  //     } 
  //   }
      
  //   if ( targetVal < yellowVal){
  //     if ( mVal <= targetVal ){
  //       mVal.addClass(green);
  //       return;
  //     }
  //     if ( mVal <= yellowVal ){
  //       mVal.addClass(yellow);
  //       return;
  //     } 
  //   }
    
  //   mVal.addClass(red);

  // }
        
  // -----------------------------------------------------------------------------
  // Buttons
  // -----------------------------------------------------------------------------
  
  refresh (){
    // if ( !itgIsNull(filteredPage) ){
    //   getData(filteredPage);
    // }
    throw new Error("Method not implemented.");
  }
  
  edit (data: any){
      
    // if (!allow || locked) { return; }
    
    // disabledAll = true;
    // var id = data.id; 
    
    // if (editValue && !data.calculated) {
    //   editBgColor = false;
    //   var idA = $(".tdA"+id); 
    //   idA.removeClass('bg-warning2').removeClass('bg-success2').removeClass('bg-danger2');       
    //   idA.empty();
      
    //   var dirVal = "only-digits";
    //   if (data.unitId == itgUnits.percentage){
    //     dirVal = "zero-to-one";
    //   }
              
    //   mVal= itgStrNullToEmpty(data.value);         
    //   var mVal2 = "<input type='text' class='form-control mVal mVal"+id+
    //               "' value='"+mVal+
    //               "' maxlength='24' ng-model='mVal"+
    //               "' ng-change='getBorderColor("+data.target+","+data.yellow+")' "+dirVal+"></input>";
                  
    //     idA.html($compile(mVal2)($scope));
    // }

    // var idB = $(".tdB"+id);
    // idB.empty();
    // idB.html("<textarea class='mExp"+id+"' rows='2' maxlength='300'>"+itgStrNullToEmpty(data.explanation)+"</textarea>");
    
    // var idC = $(".tdC"+id);
    // idC.empty();
    // idC.html("<textarea class='mAct"+id+"' rows='2' maxlength='300'>"+itgStrNullToEmpty(data.action)+"</textarea>"); 
      
    
    // $(".edit"+id).hide();
    // $(".btnEdit"+id).show();
    // $(".btnEdit").prop('disabled', true);
    // $(".btnEdit"+id).prop('disabled', false);  
    throw new Error("Method not implemented.");
  };
 
  cancel(data: any){
          
    this.disabledAll = false;
    var id = data.id; 
    
    if (this.editValue && !data.calculated) {
      
      if (this.editValue) {
        this.editBgColor = true;
        // var idA = $(".tdA"+id);  
        // idA.addClass(getBgColor(data));
        // idA.empty();
        // idA.html("<span>"+itgStrNullToEmpty(data.value)+"</span>");
      }
    }
    
    // var idB = $(".tdB"+id);
    // idB.empty();
    // idB.html("<span>"+itgStrNullToEmpty(data.explanation)+"</span>");
    
    // var idC = $(".tdC"+id);
    // idC.empty();
    // idC.html("<span>"+itgStrNullToEmpty(data.action)+"</span>");
          
    // $(".btnEdit"+id).hide();
    // $(".edit"+id).show();
  }     
     
  save (data:any){
    
    if (!this.allow || this.locked) { return; }
    
    this.showError = false;
    this.disabledAll = true;
    var msg = "";
    var id = data.id; 
    
    var mVal = data.value;
    if (this.editValue && !data.calculated) {
      //mVal = $(".mVal" + id).val();
      // if (!itgIsEmpty(mVal)) {
      //   if (!itgIsNumeric(mVal)){
      //     $(".mVal"+id).focus();
      //     msg = "Measure Value must be a Number.";
      //     return dialog.alert(title, msg);
      //   };
      // }
    } 

    // var mExp = $(".mExp"+id).val().trim();
    // var mAct = $(".mAct"+id).val().trim();
    
    // data.explanation = itgStrNullToEmpty(data.explanation);
    // data.action = itgStrNullToEmpty(data.action);
    
    // var dataNew = angular.copy(data);
    // if (itgIsEmpty(mVal)) {
    //   dataNew.value = null;
    // }
    // else {
    //   dataNew.value = Number(mVal);
    // }

    // dataNew.explanation = mExp;
    // dataNew.action = mAct;
    
    // if (angular.equals(data, dataNew)){
    //   msg = "There are no changes for <br /> '" + dataNew.name + "'.<br /> Unable to Save.";
    //   return dialog.alert(title, msg); 
    //}
    
    // Call Server - PUT
    // this.progress(true);
    // this.pages.measureData.update(
    //pages.general('/jsons/new/pages/measuredata_put.json').get(
    //   { 
    //     calendarId: calendarId,   
    //     day: null,
    //     hierarchyId: hierarchyId,
    //     measureTypeId: measureTypeId,
    //     measureDataId: data.id,
    //     measureValue: mVal,
    //     explanation: mExp,
    //     action: mAct
    //   },
    //   function (value) {
    //     if ( (itgIsNull(value.error)) && (value.data.length > 0) ) {
    //       data.value = value.data[0].value;
    //       //data.value = itgStrNullToEmpty(value.data[0].value);
    //       data.explanation = value.data[0].explanation;
    //       data.action = value.data[0].action;
    //       data.updated = value.data[0].updated;
    //       logger.logSuccess('Measure ' + data.name + ' updated.');
    //       progress(false);
    //       cancel(data);
    //     }
    //     else { 
    //       processLocalError(title, value.error.message, value.error.id, null, value.error.authError);
    //     }
    //     disabledAll = false;
    //   }, function (err) { 
    //     processLocalError(title, err.statusText, null, err.status, null); }
    // );
    
    //this.cancel(data);
  }
  
  // -----------------------------------------------------------------------------
    
  // Called from FilterCtrl only
  getMeasureData (filtered: any){  
    
    this.showError = false;
    this.disabledAll = true;
    this.dataRange = "";  

    this.filteredPage = filtered;      

    this.calendarId = filtered.calendarId;
    this.day = filtered.day;
    this.hierarchyId = filtered.hierarchyId;
    this.measureTypeId = filtered.measureTypeId;
    
    // Call Server
    this.progress(true);
    //pages.measureData.get(
    //pages.general('/jsons/new/pages/measuredata_getall.json').get(
    // this._measureDataService.getData(
    //   {
    //     calendarId: filtered.calendarId,   
    //     day: null, 
    //     hierarchyId: hierarchyId,
    //     measureTypeId: measureTypeId,
    //     measureDataId: null,
    //     measureValue: null,
    //     explanation: null,
    //     action: null 
    //   },
    //   function (value) {
    //     if (itgIsNull(value.error)) {
    //       calendarId = value.calendarId;
    //       data = value.data;
    //       dataRange = value.range;
    //       allow = value.allow;
    //       locked = value.locked;
    //       editValue= value.editValue;
    //       showActionButtons = allow && !locked;
    //       loadTable();
    //       progress(false);
    //     }
    //     else { 
    //       processLocalError(title, value.error.message, value.error.id, null, value.error.authError)
    //     }
    //     disabledAll = false;
    //   }, function (err) { 
    //   processLocalError(title, err.statusText, null, err.status, null); }
    // );
    
    this.loadTable(false);
    
  }
      
  loadTable (showErr: boolean){
    
    this.showError = false;
    try {
      this.btnDisabled = false;
      // Validates data        
      // if ( itgIsNull(data) || ( data.length == 0) ) {
      //   btnDisabled = true;
      //   if (itgIsNull(showErr)){
      //     processLocalError(title, MSG_DATA_NO_FOUND, null, null, null);
      //   }
      // }
      
      this.populateTable();
      return;

    }
    catch(err){}
    
    // No Success
    this.processLocalError(this.title, MSG_ERROR_PROCESSING, null, null, null);      
    return;
  }
       
    

}
