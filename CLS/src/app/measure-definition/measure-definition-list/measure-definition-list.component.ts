import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { MeasureDefinitionService } from 'src/app/_services/measure-definition.service';

@Component({
  selector: 'app-measure-definition-list',
  templateUrl: './measure-definition-list.component.html',
  styleUrls: ['./measure-definition-list.component.css']
})
export class MeasureDefinitionListComponent implements OnInit {

  measureDefinitionList?: any = [];

  constructor(private measureDefinitionService: MeasureDefinitionService, private router: Router, private toastr: ToastrService) { }

  ngOnInit(): void {
    this.getMeasureDefinition();
  }

  //Get Measure Definition from service ----------------------------------------------------------------
  getMeasureDefinition() {
    //throw new Error('Method not implemented.');
    this.measureDefinitionService.getMeasureDefinition().subscribe({
      next: (response: any) => {
        this.measureDefinitionList = response.data
        console.log("Measure Definition On Component: ", this.measureDefinitionList)}
    });
  }
}
