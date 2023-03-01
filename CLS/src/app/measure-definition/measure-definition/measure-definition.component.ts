import { Component, OnInit } from '@angular/core';
import { ToggleService } from 'src/app/_services/toggle.service';

@Component({
  selector: 'app-measure-definition',
  templateUrl: './measure-definition.component.html',
  styleUrls: ['./measure-definition.component.css']
})
export class MeasureDefinitionComponent implements OnInit {

  toggle: any = true;

  constructor(private toggleService: ToggleService) { }

  ngOnInit(): void {
    this.toggleService.toggle$.subscribe(toggle => {
      this.toggle = toggle;
    });
  }

}
