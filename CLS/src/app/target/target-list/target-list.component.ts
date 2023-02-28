import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TargetService } from 'src/app/_services/target.service';

@Component({
  selector: 'app-target-list',
  templateUrl: './target-list.component.html',
  styleUrls: ['./target-list.component.css']
})
export class TargetListComponent implements OnInit {

  targetList?: any = [];

  constructor(private targetService: TargetService, private router: Router, private toastr: ToastrService) { }

  ngOnInit(): void {
    this.getTargets();
  }
  getTargets1() {
    throw new Error('Method not implemented.');
  }

  //Get Measure Data from service ----------------------------------------------------------------
  getTargets() {
    //throw new Error('Method not implemented.');
    this.targetService.getTarget().subscribe({
      next: (response: any) => {
        this.targetList = response.data
        console.log("Target Component: ", this.targetList)}
    });
  }


}
