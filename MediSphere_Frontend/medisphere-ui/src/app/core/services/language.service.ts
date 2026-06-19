import { Injectable, signal, computed } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private currentLangSignal = signal<'en' | 'hi' | 'te'>('en');

  currentLang = computed(() => this.currentLangSignal());

  private translations: Record<'en' | 'hi' | 'te', Record<string, string>> = {
    en: {
      welcome: 'Welcome to MediSphere',
      symptomChecker: 'Smart Symptom Checker',
      symptomPlaceholder: 'Describe your symptoms (e.g. chest pain, skin rash, fever)...',
      findDoctors: 'Find Recommended Doctors',
      queueStatus: 'Real-time Queue Tracking',
      noActiveToken: 'You have no active token today.',
      activeToken: 'Your active token is:',
      waitingTime: 'Estimated waiting time:',
      servingToken: 'Currently serving token:',
      rewards: 'Loyalty Rewards',
      points: 'Points Balance',
      referralCode: 'Your Referral Code',
      earnMore: 'Earn 100 points for every friend referred!',
      bookAppointment: 'Book Appointment',
      selectDoctor: 'Select Doctor',
      reason: 'Reason for Visit',
      useRewards: 'Use reward points for 50% discount',
      payNow: 'Pay and Confirm',
      telemedicineRoom: 'Telemedicine Consultation Room',
      doctorResponse: 'Doctor Response',
      submitResponse: 'Submit Response',
      language: 'Language',
      dashboard: 'Dashboard',
      appointments: 'Appointments',
      logout: 'Logout',
      experience: 'Years Experience',
      rating: 'Rating',
      available: 'Available',
      unavailable: 'Busy',
      location: 'Clinic Location'
    },
    hi: {
      welcome: 'मेडिस्फेयर में आपका स्वागत है',
      symptomChecker: 'स्मार्ट लक्षण जांचकर्ता',
      symptomPlaceholder: 'अपने लक्षणों का वर्णन करें (जैसे सीने में दर्द, त्वचा पर लाल चकत्ते, बुखार)...',
      findDoctors: 'अनुशंसित डॉक्टर खोजें',
      queueStatus: 'वास्तविक समय कतार ट्रैकिंग',
      noActiveToken: 'आज आपके पास कोई सक्रिय टोकन नहीं है।',
      activeToken: 'आपका सक्रिय टोकन है:',
      waitingTime: 'अनुमानित प्रतीक्षा समय:',
      servingToken: 'वर्तमान में सेवारत टोकन:',
      rewards: 'लॉयल्टी पुरस्कार',
      points: 'अंकों का संतुलन',
      referralCode: 'आपका रेफरल कोड',
      earnMore: 'रेफ़र किए गए प्रत्येक मित्र के लिए 100 अंक अर्जित करें!',
      bookAppointment: 'अप्वाइंटमेंट बुक करें',
      selectDoctor: 'डॉक्टर चुनें',
      reason: 'यात्रा का कारण',
      useRewards: '50% छूट के लिए पुरस्कार अंकों का उपयोग करें',
      payNow: 'भुगतान करें और पुष्टि करें',
      telemedicineRoom: 'टेलीमेडिसिन परामर्श कक्ष',
      doctorResponse: 'डॉक्टर की प्रतिक्रिया',
      submitResponse: 'प्रतिक्रिया भेजें',
      language: 'भाषा',
      dashboard: 'डैशबोर्ड',
      appointments: 'नियुक्तियाँ',
      logout: 'लॉगआउट',
      experience: 'वर्षों का अनुभव',
      rating: 'रेटिंग',
      available: 'सुलभ',
      unavailable: 'व्यस्त',
      location: 'क्लินिक का स्थान'
    },
    te: {
      welcome: 'మెడిస్పియర్ కు స్వాగతం',
      symptomChecker: 'స్మార్ట్ లక్షణాల గుర్తింపు',
      symptomPlaceholder: 'మీ లక్షణాలను వివరించండి (ఉదా. గుండె నొప్పి, చర్మంపై దద్దుర్లు, జ్వరం)...',
      findDoctors: 'సిఫార్సు చేయబడిన వైద్యులను కనుగొనండి',
      queueStatus: 'రియల్ టైమ్ క్యూ ట్రాకింగ్',
      noActiveToken: 'ఈరోజు మీకు యాక్టివ్ టోకెన్ లేదు.',
      activeToken: 'మీ యాక్టివ్ టోకెన్:',
      waitingTime: 'అంచనా వేయబడిన నిరీక్షణ సమయం:',
      servingToken: 'ప్రస్తుతం సేవలు అందిస్తున్న టోకెన్:',
      rewards: 'లాయల్టీ బహుమతులు',
      points: 'పాయింట్ల బ్యాలెన్స్',
      referralCode: 'మీ రెఫరల్ కోడ్',
      earnMore: 'స్నేహితుడిని రెఫర్ చేసినందుకు 100 పాయింట్లు పొందండి!',
      bookAppointment: 'అపాయింట్‌మెంట్ బుక్ చేయండి',
      selectDoctor: 'వైద్యుడిని ఎంచుకోండి',
      reason: 'సందర్శన కారణం',
      useRewards: '50% డిస్కౌంట్ కోసం రివార్డ్ పాయింట్లను ఉపయోగించండి',
      payNow: 'చెల్లించి నిర్ధారించండి',
      telemedicineRoom: 'టెలిమెడిసిన్ కన్సల్టేషన్ రూమ్',
      doctorResponse: 'వైద్యుని స్పందన',
      submitResponse: 'స్పందన సమర్పించు',
      language: 'భాష',
      dashboard: 'డ్యాష్‌బోర్డ్',
      appointments: 'అపాయింట్‌మెంట్‌లు',
      logout: 'లాగ్అవుట్',
      experience: 'సంవత్సరాల అనుభవం',
      rating: 'రేటింగ్',
      available: 'అందుబాటులో ఉంది',
      unavailable: 'బిజీ',
      location: 'క్లినిక్ స్థానం'
    }
  };

  setLanguage(lang: 'en' | 'hi' | 'te') {
    this.currentLangSignal.set(lang);
  }

  translate(key: string): string {
    const lang = this.currentLangSignal();
    return this.translations[lang]?.[key] || key;
  }
}
