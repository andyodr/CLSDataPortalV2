import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { MeasureDataService } from 'src/app/_services/measure-data.service';

@Component({
  selector: 'app-measure-list',
  templateUrl: './measure-list.component.html',
  styleUrls: ['./measure-list.component.css']
})
export class MeasureListComponent implements OnInit {

  measureList?: any = [];

  constructor(private measureService: MeasureDataService, private router: Router, private toastr: ToastrService) { }

  ngOnInit(): void {
    this.getMeasure();
  }

  //Get Measure from Measure Service ----------------------------------------------------------------
  getMeasure() {
    //throw new Error('Method not implemented.');
    this.measureService.getMeasureData().subscribe({
      next: (response: any) => {
        this.measureList = response.data
        console.log("Measure On Component: ", this.measureList)}
    });
  }

}
