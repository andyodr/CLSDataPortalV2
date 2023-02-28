import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { MeasureData } from 'src/app/_models/measureData';
import { MeasureDataService } from 'src/app/_services/measure-data.service';

@Component({
  selector: 'app-measure-data-list',
  templateUrl: './measure-data-list.component.html',
  styleUrls: ['./measure-data-list.component.css']
})
export class MeasureDataListComponent implements OnInit {

  measureDataList?: any = [];

  constructor(private measureDataService: MeasureDataService, private router: Router, private toastr: ToastrService) { }

  ngOnInit(): void {
    this.getMeasureData();
  }

  //Get Measure Data from service ----------------------------------------------------------------
  getMeasureData() {
    //throw new Error('Method not implemented.');
    this.measureDataService.getMeasureData().subscribe({
      next: (response: any) => {
        this.measureDataList = response.data
        console.log("Measure Data On Component: ", this.measureDataList)}
    });
  }

}
