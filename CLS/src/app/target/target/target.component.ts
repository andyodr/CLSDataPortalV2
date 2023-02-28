import { Component, OnInit } from '@angular/core';
import { ToggleService } from 'src/app/_services/toggle.service';

@Component({
  selector: 'app-target',
  templateUrl: './target.component.html',
  styleUrls: ['./target.component.css']
})
export class TargetComponent implements OnInit {

  toggle: any = true;

  constructor(private toggleService: ToggleService) { }

  ngOnInit(): void {
    this.toggleService.toggle$.subscribe(toggle => {
      this.toggle = toggle;
    });
  }

}
