import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { processError } from '../lib/app-constants';
import { Data, MeasureApiParams, MeasureApiResponse } from '../_models/measure';
import { RegionFilter } from '../_models/regionhierarchy';
import { LoggerService } from '../_services/logger.service';
import { MeasureService } from '../_services/measure.service';

@Component({
  selector: 'app-measures',
  templateUrl: './measures.component.html',
  styleUrls: ['./measures.component.scss']
})
export class MeasuresComponent implements OnInit {


  measureResponse: MeasureApiResponse | undefined;
  @Output() progressEvent = new EventEmitter<boolean>();

  //Declarations
  title = "Measures"
  showContentPage = true;
  filterDisplay = {
    intervals: false,
    measureTypes: true,
    hierarchy: true
  };
  data: Data [] = [];
  hierarchyId: number | null = null;
  measureTypeId: number | null = null;
  disabledAll = true;
  btnDisabled = false;
  skTargets = "";
  allow = false;
  confirmed = false;
  dataConfirmed: any = {
    target: null,
    yellow: null,
    data: null,
    isApplyToChildren: false,
    isCurrentUpdate: false,
    confirmIntervals: null,
    targetId: null,
    targetCount: null
  };
  filters: any = null;
  errorMsg: any = "";
  showError = false;
  //for Mat Table
  dataSource = new MatTableDataSource([] as Data[])
  displayedColumns = ["name", "parentName", "level", "active"]
  @ViewChild(MatSort) sort!: MatSort
  //for Mat Drawer
  private userSubscription = new Subscription()
  hierarchy: RegionFilter[] = []
  hierarchyLevels!: { name: string, id: number }[]
  model = {
      id: 0,
      active: false,
      name: "",
      level: 0,
      selectedParent: null as number | number[] | null
  }
  filtered: MeasureApiParams = {
    calendarId: 649,
    hierarchyId: 1,
    measureTypeId: 1,
  }

  constructor(private measureService: MeasureService, public logger: LoggerService ) { }

  ngOnDestroy(): void {
      this.userSubscription.unsubscribe()
  }

  ngOnInit(): void {

      console.log("target init");
      //this.targetService.getTarget()
      this.getMeasures(this.filtered)
  }

 getMeasures(filtered: MeasureApiParams): void {
      this.showError = false;
      this.disabledAll = true;
      //this.data = null;
      console.log("get target on the component");
      // Call Server
      this.measureService.getMeasure2(filtered).subscribe({
          next: response => {
            this.measureResponse = response;
            this.data = response.data
            console.log("Measures on Component: ", response);
            this.dataSource = new MatTableDataSource(response.data)
            this.dataSource.sort = this.sort
            this.allow = response.allow
            //this.confirmed = response.confirmed
            this.dataSource = new MatTableDataSource(response.data)
            this.dataSource.sort = this.sort
            this.disabledAll = false;
          },
          error: (err: any) => {
              this.processLocalError(this.title, err.error.message, err.error.id, null, err.error.authError)
          }
      })
  }


  
  // getFiltersFromMain(): void {
  //   this.showError = false;
  //   this.disabledAll = true;
  //   this.filters = null;
  //   this.progress(true);
  //   // Call Server
  //   pages.targetsFilter.get(
  //     {},
  //     value => {
  //       if (itgIsNull(value.error)) {
  //         this.filters = value;
  //         this.$broadcast('filtersEvent');
  //         this.progress(false);
  //       } else {
  //         this.processLocalError('Filters', value.error.message, value.error.id, null, value.error.authError);
  //       }
  //       this.disabledAll = false;
  //     },
  //     err => {
  //       this.processLocalError('Filters', err.statusText, null, err.status, null);
  //     }
  //   );
  // }

 edit(data: any) {
      this.skTargets = "edit";
  }
  save(data: any) {
      this.skTargets = "save";
  }
  cancel(data: any) {
      this.skTargets = "cancel";
  }

  applyToChildren() {
      this.dataConfirmed.isApplyToChildren = true;
  }

  applyFilter(event: Event) {
      const filterValue = (event.currentTarget as HTMLInputElement).value
      this.dataSource.filter = filterValue.trim().toLowerCase()
  }

  identity(_: number, item: { id: number }) {
      return item.id
  }

  processLocalError(name: string, message: string, id: any, status: unknown, authError: any) {
      this.errorMsg = processError(name, message, id, status)
      this.showError = true
      this.disabledAll = false
      this.showContentPage = (authError != true)
  }

  closeError() {
      this.errorMsg = ""
      this.showError = false
  }

}
