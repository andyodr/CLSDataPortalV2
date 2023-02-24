import { Component, OnInit } from '@angular/core';
import { ToggleService } from '../../_services/toggle.service';

@Component({
  selector: 'app-measuredata',
  templateUrl: './measuredata.component.html',
  styleUrls: ['./measuredata.component.css']
})
export class MeasureDataComponent implements OnInit {
  toggle: any = true;
  constructor(private toggleService: ToggleService) { }

  ngOnInit(): void {
    this.toggleService.toggle$.subscribe(toggle => {
      this.toggle = toggle;
    });
  }

}
