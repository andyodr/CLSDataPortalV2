import { Component, OnInit } from '@angular/core';
import { ToggleService } from 'src/app/_services/toggle.service';

@Component({
  selector: 'app-hierarchy',
  templateUrl: './hierarchy.component.html',
  styleUrls: ['./hierarchy.component.css']
})
export class HierarchyComponent implements OnInit {

  toggle: any = true;

  constructor(private toggleService: ToggleService) { }

  ngOnInit(): void {
    this.toggleService.toggle$.subscribe(toggle => {
      this.toggle = toggle;
    });
  }

}
