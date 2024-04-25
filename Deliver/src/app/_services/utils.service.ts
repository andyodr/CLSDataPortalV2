import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UtilsService {

  itgIsNull(data: any): boolean {
    return ((data === undefined) || (data === 'undefined') || (data === null) || (data === 'null') || (isNaN(data)));
  }
}
