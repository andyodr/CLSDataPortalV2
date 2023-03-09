import { Injectable, Directive, HostListener } from "@angular/core"
import { Location } from "@angular/common"
import { Router, NavigationEnd, ActivatedRoute } from "@angular/router"

@Injectable({ providedIn: "root" })
export class NavigationService {
    private history: string[] = []

    constructor(private router: Router, private location: Location) {
        this.router.events.subscribe((event) => {
            if (event instanceof NavigationEnd) {
                this.history.push(event.urlAfterRedirects)
            }
        });
    }

    back(): void {
        this.history.pop()
        if (this.history.length > 0) {
            this.location.back()
        }
        else {
            this.router.navigate([".."])
        }
    }
}

@Directive({
    selector: "[navigateBack]",
})
export class NavigateBackDirective {
    constructor(private navigation: NavigationService, private route: ActivatedRoute) { }

    @HostListener("click")
    onClick(): void {
        this.navigation.back()
    }
}
