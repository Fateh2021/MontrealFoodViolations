import { createApp } from 'vue';
import App from './App.vue';
import './styles.css';

const savedTheme = localStorage.getItem('theme');
const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
document.documentElement.dataset.theme = savedTheme ?? (prefersDark ? 'dark' : 'light');

createApp(App).mount('#app');
