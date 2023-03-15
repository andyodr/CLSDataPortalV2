import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UtilsService {

  //constructor() { }

  // itgIsNull(data: any): boolean {
  //   return data === undefined || data === null || data !== data;
  // }

  isNull(data: any): boolean {
    return data === undefined || data === null || Number.isNaN(data);
  }

  isEmpty(data: any): boolean {
    if (!this.isNull(data)) {
      const str = data.toString().trim();
      return str.length === 0;
    }
    return true;
  }

  itgIsNull(data: any): boolean {
    return ((data === undefined) || (data === 'undefined') || (data === null) || (data === 'null') || (isNaN(data)));
  }
  
  itgIsEmpty(data: string): boolean {
    let result = true;
    if (!this.itgIsNull(data)) {
      const d = data.trim();
      if (d.length > 0) {
        result = false;
      }
    }
    return result;
  }
  
  itgIsNumeric(data: any): boolean {
    return !isNaN(parseFloat(data)) && isFinite(data);
  }
  
  itgGetMonthObj(id: any): any {
    let month = null;
    let i = 0;
    /*for (i in itgMonths){
      if (itgMonths[i].id === id){
        break;
      } 
    }
    return itgMonths[i];*/
  }

  itgGetMonthName(id: number, isAbrv: boolean = false): string {
    const month = this.itgGetMonthObj(id);
    if (month) {
      return isAbrv ? month.abrv : month.name;
    }
    return '';
  }

  itgGetMonthNameShort(id: number): string {
    return this.itgGetMonthName(id, true);
  }

  itgGetCurrentMonth(): any {
    const id = new Date().getMonth() + 1;
    return this.itgGetMonthObj(id);
  }

  itgGetCurrentMonthName(): string {
    const month = new Date().getMonth() + 1;
    return this.itgGetMonthName(month);
  }

  itgGetCurrentMonthNameShort(): string {
    const id = new Date().getMonth() + 1;
    return this.itgGetMonthName(id, true);
  }

  itgGetCurrentYear(): number {
    return new Date().getFullYear();
  }

  itgGetStr(data: any): string {
    return data ? data : '';
  }

  itgGetStrFromNum(data: number): string {
    return this.itgGetStr(data.toString());
  }

//   <div *ngIf="showTable; else notTable">
//   <div class="itgTable"></div>
// </div>
// <ng-template #notTable>
//   <div class="itgTableNot"></div>
// </ng-template>
  showTable = false;
  itgShowTable(bool: boolean) {
    this.showTable = bool;
  }

  /*processError(heading: string, message: string, id: string | null, status: string | null) {
    const errorMsg = {
      heading: null,
      message: null,
      id: null,
      status: null
    };
    if (!this.itgIsNull(errorMsg)){
      errorMsg.heading = 'Error: ' + heading;
    }
     
    errorMsg.heading = 'Error: ' + heading;
    
    if (id !== null){
      errorMsg.id = 'Error ID: ' + id;
    }
    if (status !== null){
      errorMsg.status = 'Error Status: ' + status;
    }
    
    errorMsg.message = 'Error Message: ' + message;
    
    return errorMsg;
  }*/
  
  /*itgStrToBool(str: string): boolean {
    return ( (str === "true") || (str === true) );
  }*/
  
  itgBoolToStr(bool: boolean): string {
    let str = "false";
    if (bool === true){
      str = "true";
    }
    return str; 
  }
  
  itgStrNullToEmpty(str: string): string {
    let ret = str;
    if (str === null || str === "null"){
      ret = "";
    }
    return ret; 
  }


  
}
