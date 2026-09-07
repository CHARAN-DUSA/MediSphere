import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface RazorpayOrder {
  orderId: string;
}

export interface PaymentConfig {
  keyId: string;
  isSandbox: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {

  private baseUrl = `${environment.apiUrl}/payments`;

  private razorpayLoaded = false;

  constructor(private http: HttpClient) {}

  /** Fetches Razorpay public key ID and sandbox flag from the server. */
  getPaymentConfig() {
    return this.http.get<ApiResponse<PaymentConfig>>(
      `${this.baseUrl}/config`
    );
  }

  createOrder(appointmentId: number, amount: number) {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/create-order`,
      {
        appointmentId,
        amount
      }
    );
  }

  simulateWebhook(
    orderId: string,
    paymentId: string,
    amount: number
  ) {
    return this.http.post<ApiResponse<object>>(
      `${this.baseUrl}/simulate-webhook`,
      {
        orderId,
        paymentId,
        amount
      }
    );
  }

  reportPaymentFailed(
    orderId: string,
    paymentId?: string
  ) {
    return this.http.post<ApiResponse<object>>(
      `${this.baseUrl}/payment-failed`,
      {
        orderId,
        paymentId
      }
    );
  }

  private loadRazorpayScript(): Promise<void> {
    return new Promise((resolve, reject) => {

      if (this.razorpayLoaded || (window as any).Razorpay) {
        this.razorpayLoaded = true;
        resolve();
        return;
      }

      const script = document.createElement('script');

      script.src =
        'https://checkout.razorpay.com/v1/checkout.js';

      script.async = true;

      script.onload = () => {
        this.razorpayLoaded = true;
        resolve();
      };

      script.onerror = () => {
        reject(
          new Error('Failed to load Razorpay SDK')
        );
      };

      document.body.appendChild(script);
    });
  }

  async launchRazorpayCheckout(
    orderId: string,
    amount: number,
    keyId: string,
    prefillName: string,
    prefillEmail: string
  ): Promise<string> {

    await this.loadRazorpayScript();

    return new Promise((resolve, reject) => {

      const options = {
        key: keyId,
        amount: amount * 100,
        currency: 'INR',
        name: 'MediSphere',
        description: 'Medical Consultation Fee',
        order_id: orderId,

        prefill: {
          name: prefillName,
          email: prefillEmail
        },

        theme: {
          color: '#1e3a8a'
        },

        handler: (response: any) => {
          resolve(response.razorpay_payment_id);
        },

        modal: {
          ondismiss: () => {
            reject('Payment dismissed');
          }
        }
      };

      const rzp = new (window as any).Razorpay(options);

      rzp.open();
    });
  }
}