import { Component, OnInit, OnDestroy } from "@angular/core"
import { Subscription } from "rxjs"
import { User } from "src/app/_models/user"
import { ToggleService } from "src/app/_services/toggle.service"
import { UserService } from "src/app/_services/user.service"

@Component({
    selector: "app-user-list",
    templateUrl: "./userlist.component.html",
    styleUrls: ["./userlist.component.css"]
})
export class UserListComponent implements OnInit, OnDestroy {
    users?: User[] | undefined
    private userSubscription = new Subscription()
    toggle: any = true

    constructor(private userService: UserService, private toggleService: ToggleService) { }

    ngOnDestroy(): void {
        this.userSubscription.unsubscribe()
    }

    ngOnInit(): void {
        this.getUsers()
        this.toggleService.toggle$.subscribe(toggle => {
            this.toggle = toggle
        })
    }

    //Get Users from service ----------------------------------------------------------------

    getUsers() {
        this.userSubscription = this.userService.getUsers().subscribe({
            next: (response: any) => {
                this.users = response.data
                console.log("Users On Component: ", this.users)
            },
            error: (err: any) => console.log(err),
            complete: () => console.log("Request Completed")
        })
    }


    /*getUsers(){
      this.http.get(environment.baseUrl + 'api/users').subscribe({
        next: response => this.users = response,
        error: err => console.log(err),
        complete: () => console.log('Resquest Completed')
       });
    }*/

    /*getUsers(): void {
      this.userService.getUsers()
      .subscribe(users => this.users = users);
    }*/



    /*getUsers(){
      return this.userService.getUsers().subscribe((data: any[]) => {
        this.users = data;
      });
    }*/


    /*getUsers(){
      this.userService.getUsers().subscribe((data: any[]) => {
        this.users = data;
      });
    }*/

    //Get Users from service
    /*getUsers(){
      this.userService.getUsers().subscribe((data: any[]) => {
        this.users = data;
      });
    }*/





    /*getUsersOnComponent(){
      return this.userService.getUsers().subscribe(users => {
        this.users = users
        console.log("Users on component: ", this.users);
      }, error => {
        console.log(error);
      }, () => {
        console.log('Resquest Completed');
      })
    }*/

}
