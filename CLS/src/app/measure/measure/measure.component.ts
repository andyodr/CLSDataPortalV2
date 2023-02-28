import { Component, OnInit } from '@angular/core';
import { ToggleService } from 'src/app/_services/toggle.service';

@Component({
  selector: 'app-measure',
  templateUrl: './measure.component.html',
  styleUrls: ['./measure.component.css']
})
export class MeasureComponent implements OnInit {

  toggle: any = true;

  constructor(private toggleService: ToggleService) { }

  ngOnInit(): void {
    this.toggleService.toggle$.subscribe(toggle => {
      this.toggle = toggle;
    });
  }

}
