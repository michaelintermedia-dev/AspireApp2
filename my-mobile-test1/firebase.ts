import { initializeApp } from 'firebase/app';

const firebaseConfig = {
  apiKey: 'AIzaSyAiB_dJtdRKoIMfKL3kqwy62wtZVCQwSpU',
  authDomain: 'my-mobile-test1-5dcf5.firebaseapp.com',
  projectId: 'my-mobile-test1-5dcf5',
  storageBucket: 'my-mobile-test1-5dcf5.firebasestorage.app',
  messagingSenderId: '347088754912',
  appId: '1:347088754912:android:cbb18b984e3b7bc61e1885',
};

export const firebaseApp = initializeApp(firebaseConfig);