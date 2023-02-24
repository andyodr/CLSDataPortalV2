import { Component, OnInit } from '@angular/core';
import { ToggleService } from '../../_services/toggle.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit {

  public  opened = true;

  constructor(private toogleService: ToggleService) { }

  ngOnInit(): void {
  }

  toggle() {
    this.opened = !this.opened;
    this.toogleService.setToggle(this.opened);
  }

}
