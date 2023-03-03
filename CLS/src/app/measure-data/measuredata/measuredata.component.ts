import { Component, OnInit } from '@angular/core';
import { ToggleService } from '../../_services/toggle.service';

@Component({
  selector: 'app-measuredata',
  templateUrl: './measuredata.component.html',
  styleUrls: ['./measuredata.component.css']
})
export class MeasureDataComponent implements OnInit {
  
  toggle: any = true;

  constructor(private toggleService: ToggleService ) { }

  ngOnInit(): void {
    this.toggleService.toggle$.subscribe(toggle => {
      this.toggle = toggle;
    });
  }






  
  /*title = 'Measure Data';
  showContentPage = true;
  filterDisplay = {
    intervals: true,
    measureTypes: true,
    hierarchy: true      
  };
  dataRange = "";
  data = [];
  rendered = function(){};
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
  
  isBoolShow(str:any){
    return ((str == "true") || (str == true));
  }
    
  toggleFilterOpen() {
    $mdSidenav('left').toggle();
  };

  progress (bool){
    this.$broadcast('ProgressEvent', bool);
  }
  
  // Reloads table from TableCtrl
  populateTable = function(){
    this.$broadcast('mmTableEvent', this.selCalSelected.id); 
  }
          
  errorMsg = "";
  showError = false;
  closeError(){
    this.errorMsg = "";
    this.showError = false;
  }
  processLocalError = (title, message, id, status, authError){
    this.errorMsg = processError(title, message, id, status);
    this.progress(false);
    this.showError = true;
    this.disabledAll = false;
    this.showContentPage = ( authError != true );
  }
  
  // Make a call for filters
  this.filters = null;
  this.getFiltersFromMain = function(){
    this.showError = false;
    this.disabledAll = true;
    this.filters = null;
    this.progress(true);
    // Call Server
    pages.measureDataFilter.get(
    //pages.general('/jsons/new/pages/filters.json').get(
      {},
      function (value) {
        if (itgIsNull(value.error)) {
          this.filters = value;
          this.$broadcast('filtersEvent');
          this.progress(false);
        }
        else { 
          this.processLocalError('Filters', value.error.message, value.error.id, null, value.error.authError)
        }
        this.disabledAll = false;
      }, function (err) { 
        this.processLocalError('Filters', err.statusText, null, err.status, null) }
    );
  }  
  
  // -----------------------------------------------------------------------------
  
  // Popover
  this.dPopover = {
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
  
  mmPopover = (data: any){    
    this.dPopover.id = data.id;
    this.dPopover.title = data.name;
    this.dPopover.value = itgStrNullToEmpty(data.value);
    this.dPopover.target = itgStrNullToEmpty(data.target);
    this.dPopover.yellow = itgStrNullToEmpty(data.yellow);
    this.dPopover.expression = data.expression;
    this.dPopover.evaluated = data.evaluated;
    this.dPopover.description = data.description;
    this.dPopover.calculated = data.calculated;
  };
  
  // Selection Calculated    
  this.selCalculated = [
    {id:0, name: "Calculated"},
    {id:1, name: "Manual"},
    {id:2, name: "Manual and Calculated"}
  ];
  this.selCalSelected = this.selCalculated[itgSelCal.all];
  this.onselCalChange = function(){
    this.populateTable(); 
  }     

  // Measure Value Bg colors
  getBgColor(data: any){
         
    if ( !this.editBgColor ) return "";   
    if ( itgIsEmpty(data.value) ) return "";
    if ( itgIsNull(data.target) && itgIsNull(data.yellow) ) return "";
    
    var red = "bg-danger2";
    var yellow = "bg-warning2";
    var green = "bg-success2";
    var result = red; 
          
    if ( itgIsNull(data.target) ) {
      if ( data.value >= data.yellow ) return green;
      return result;
    }
    
    if ( itgIsNull(data.yellow) ){
      if ( data.value >= data.target ) return green;
      return result;
    }
    
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
  
  getBorderColor(targetVal, yellowVal){
     
    var red = "border-danger2";
    var yellow = "border-warning2";
    var green = "border-success2";

    var mVal = $(".mVal");
    mVal.removeClass(red).removeClass(yellow).removeClass(green);
    if ( itgIsEmpty(this.mVal) ) return;
         
    if ( itgIsNull(targetVal) ) {
      if ( this.mVal >= yellowVal ){
          mVal.addClass(green);
          return;
      }
      mVal.addClass(red);
      return;
    }
    
    if ( itgIsNull(yellowVal) ){
      if ( this.mVal >= targetVal ) {
        mVal.addClass(green);
        return;
      }
      mVal.addClass(red);
      return;
    }
    
    if ( targetVal >= yellowVal ){
      if ( this.mVal >= targetVal ){
        mVal.addClass(green);
        return;
      } 
      if ( this.mVal >= yellowVal ){
        mVal.addClass(yellow);
        return;
      } 
    }
     
    if ( targetVal < yellowVal){
      if ( this.mVal <= targetVal ){
        mVal.addClass(green);
        return;
      }
      if ( this.mVal <= yellowVal ){
        mVal.addClass(yellow);
        return;
      } 
    }
    
    mVal.addClass(red);

  }
      
  // -----------------------------------------------------------------------------
  // Buttons
  // -----------------------------------------------------------------------------
  
  refresh (){
    if ( !itgIsNull(this.filteredPage) ){
      this.getData(this.filteredPage);
    }
  }
  
  edit(data: any){
     
    if (!this.allow || this.locked) { return; }
    
    this.disabledAll = true;
    var id = data.id; 
    
    if (this.editValue && !data.calculated) {
      this.editBgColor = false;
      var idA = $(".tdA"+id); 
      idA.removeClass('bg-warning2').removeClass('bg-success2').removeClass('bg-danger2');       
      idA.empty();
      
      var dirVal = "only-digits";
      if (data.unitId == itgUnits.percentage){
        dirVal = "zero-to-one";
      }
              
      this.mVal= itgStrNullToEmpty(data.value);         
      var mVal2 = "<input type='text' class='form-control mVal mVal"+id+
                 "' value='"+this.mVal+
                 "' maxlength='24' ng-model='mVal"+
                 "' ng-change='getBorderColor("+data.target+","+data.yellow+")' "+dirVal+"></input>";
                 
       idA.html($compile(mVal2)($scope));
    }

    var idB = $(".tdB"+id);
    idB.empty();
    idB.html("<textarea class='mExp"+id+"' rows='2' maxlength='300'>"+itgStrNullToEmpty(data.explanation)+"</textarea>");
    
    var idC = $(".tdC"+id);
    idC.empty();
    idC.html("<textarea class='mAct"+id+"' rows='2' maxlength='300'>"+itgStrNullToEmpty(data.action)+"</textarea>"); 
      
    
    $(".edit"+id).hide();
    $(".btnEdit"+id).show();
    $(".btnEdit").prop('disabled', true);
    $(".btnEdit"+id).prop('disabled', false);  
    
  };

  cancel(data: any){
          
    this.disabledAll = false;
    var id = data.id; 
    
    if (this.editValue && !data.calculated) {
      
      if (this.editValue) {
        this.editBgColor = true;
        var idA = $(".tdA"+id);  
        idA.addClass(this.getBgColor(data));
        idA.empty();
        idA.html("<span>"+itgStrNullToEmpty(data.value)+"</span>");
      }
    }
    
    var idB = $(".tdB"+id);
    idB.empty();
    idB.html("<span>"+itgStrNullToEmpty(data.explanation)+"</span>");
    
    var idC = $(".tdC"+id);
    idC.empty();
    idC.html("<span>"+itgStrNullToEmpty(data.action)+"</span>");
          
    $(".btnEdit"+id).hide();
    $(".edit"+id).show();
  }     
   
  save = function(data){
    
    if (!this.allow || this.locked) { return; }
    
    this.showError = false;
    this.disabledAll = true;
    var msg = "";
    var id = data.id; 
    
    var mVal = data.value;
    if (this.editValue && !data.calculated) {
      mVal = $(".mVal" + id).val();
      if (!itgIsEmpty(mVal)) {
        if (!itgIsNumeric(mVal)){
          $(".mVal"+id).focus();
          msg = "Measure Value must be a Number.";
          return dialog.alert(this.title, msg);
        };
      }
    } 

    var mExp = $(".mExp"+id).val().trim();
    var mAct = $(".mAct"+id).val().trim();
    
    data.explanation = itgStrNullToEmpty(data.explanation);
    data.action = itgStrNullToEmpty(data.action);
    
    var dataNew = angular.copy(data);
    if (itgIsEmpty(mVal)) {
      dataNew.value = null;
    }
    else {
      dataNew.value = Number(mVal);
    }

    dataNew.explanation = mExp;
    dataNew.action = mAct;
    
    if (angular.equals(data, dataNew)){
      msg = "There are no changes for <br /> '" + dataNew.name + "'.<br /> Unable to Save.";
      return dialog.alert(this.title, msg); 
    }
    
    // Call Server - PUT
    this.progress(true);
    pages.measureData.update(
    //pages.general('/jsons/new/pages/measuredata_put.json').get(
      { 
        calendarId: this.calendarId,   
        day: null,
        hierarchyId: this.hierarchyId,
        measureTypeId: this.measureTypeId,
        measureDataId: data.id,
        measureValue: mVal,
        explanation: mExp,
        action: mAct
      },
      function (value) {
        if ( (itgIsNull(value.error)) && (value.data.length > 0) ) {
          data.value = value.data[0].value;
          //data.value = itgStrNullToEmpty(value.data[0].value);
          data.explanation = value.data[0].explanation;
          data.action = value.data[0].action;
          data.updated = value.data[0].updated;
          logger.logSuccess('Measure ' + data.name + ' updated.');
          this.progress(false);
          this.cancel(data);
        }
        else { 
          this.processLocalError(this.title, value.error.message, value.error.id, null, value.error.authError);
        }
        this.disabledAll = false;
      }, function (err) { 
        this.processLocalError(this.title, err.statusText, null, err.status, null); }
    );
    
    this.cancel(data);
  }
  
  // -----------------------------------------------------------------------------
  
  // Called from FilterCtrl only
  this.getData = function(filtered){  
    
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
    pages.measureData.get(
    //pages.general('/jsons/new/pages/measuredata_getall.json').get(
      {
        calendarId: filtered.calendarId,   
        day: null, 
        hierarchyId: this.hierarchyId,
        measureTypeId: this.measureTypeId,
        measureDataId: null,
        measureValue: null,
        explanation: null,
        action: null 
      },
      function (value) {
        if (itgIsNull(value.error)) {
          this.calendarId = value.calendarId;
          this.data = value.data;
          this.dataRange = value.range;
          this.allow = value.allow;
          this.locked = value.locked;
          this.editValue= value.editValue;
          this.showActionButtons = this.allow && !this.locked;
          this.loadTable();
          this.progress(false);
        }
        else { 
          this.processLocalError(this.title, value.error.message, value.error.id, null, value.error.authError)
        }
        this.disabledAll = false;
      }, function (err) { 
      this.processLocalError(this.title, err.statusText, null, err.status, null); }
    );
    
    this.loadTable(false);
   
  }
      
  this.loadTable = function(showErr){
    
    this.showError = false;
    try {
      this.btnDisabled = false;
      // Validates data        
      if ( itgIsNull(this.data) || ( this.data.length == 0) ) {
        this.btnDisabled = true;
        if (itgIsNull(showErr)){
          this.processLocalError(this.title, MSG_DATA_NO_FOUND, null, null, null);
        }
      }
      
      this.populateTable();
      return;

    }
    catch(err){}
    
    // No Success
    this.processLocalError(this.title, MSG_ERROR_PROCESSING, null, null, null);
          
    return;
  }*/

}
