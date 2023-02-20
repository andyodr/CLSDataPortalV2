import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  registerMode = false;
  users: any;

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    this.getUsers();
  }

  registerToggle() {
    this.registerMode = !this.registerMode;
  }

  getUsers(){
    this.http.get(environment.baseUrl + 'api/users').subscribe({
      next: response => this.users = response,
      error: err => console.log(err),
      complete: () => console.log('Resquest Completed')
     });
  }

  cancelRegisterMode(event:boolean) {
    this.registerMode = event;
  }

}
