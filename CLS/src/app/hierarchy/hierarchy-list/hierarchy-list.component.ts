import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { HierarchyService } from 'src/app/_services/hierarchy.service';

@Component({
  selector: 'app-hierarchy-list',
  templateUrl: './hierarchy-list.component.html',
  styleUrls: ['./hierarchy-list.component.css']
})
export class HierarchyListComponent implements OnInit {

  hierarchies?: any = [];

  constructor(private hierarchyService: HierarchyService, private router: Router, private toastr: ToastrService) { }

  ngOnInit(): void {
    this.getHierarchy();
  }

  //Get Measure Data from service ----------------------------------------------------------------
  getHierarchy() {
    //throw new Error('Method not implemented.');
    this.hierarchyService.getHierarchy().subscribe({
      next: (response: any) => {
        this.hierarchies= response.data
        console.log("Hierarchy On Component: ", this.hierarchies)}
    });
  }
}
