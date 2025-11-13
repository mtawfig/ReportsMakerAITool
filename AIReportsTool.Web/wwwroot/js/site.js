// =======================
// AI Reports Tool - Main JavaScript
// Handles all client-side interactions
// =======================

// Global state
let selectedAppId = null;
let selectedAppName = null;
let selectedFormat = null;
let lastResponseText = null;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    initializeApp();
});

// Initialize application
function initializeApp() {
    loadApplications();
    loadRecents();
    setupEventListeners();
    setupHomeLink();
}

// Setup home link to show landing page
function setupHomeLink() {
    const homeLinks = document.querySelectorAll('a[href="/"]');
    homeLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            showLandingPage();
        });
    });
}

// Show landing page
function showLandingPage() {
    const landingPage = document.getElementById('landing-page');
    const formSection = document.getElementById('request-form-section');

    if (landingPage && formSection) {
        landingPage.style.display = 'block';
        formSection.style.display = 'none';

        // Clear form
        clearForm();

        // Remove active class from all sidebar links
        const allLinks = document.querySelectorAll('.nav-link');
        allLinks.forEach(link => link.classList.remove('active'));

        // Add active class to home link
        const homeLink = document.querySelector('a[href="/"]');
        if (homeLink) {
            homeLink.classList.add('active');
        }
    }
}

// Show form section
function showFormSection() {
    const landingPage = document.getElementById('landing-page');
    const formSection = document.getElementById('request-form-section');

    if (landingPage && formSection) {
        landingPage.style.display = 'none';
        formSection.style.display = 'block';

        // Scroll to top of form
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
}

// Clear form
function clearForm() {
    selectedAppId = null;
    selectedAppName = null;
    selectedFormat = null;

    document.getElementById('selected-appid').value = '';
    document.getElementById('selected-appname').value = '';
    document.getElementById('subject').value = '';
    document.getElementById('request-text').value = '';
    document.getElementById('response-container').style.display = 'none';
    document.getElementById('share-btn').disabled = true;

    // Clear format selection
    const formatButtons = document.querySelectorAll('.format-btn');
    formatButtons.forEach(btn => btn.classList.remove('selected'));
}

// Setup all event listeners
function setupEventListeners() {
    // Format button selection
    const formatButtons = document.querySelectorAll('.format-btn');
    formatButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            formatButtons.forEach(b => b.classList.remove('selected'));
            this.classList.add('selected');
            selectedFormat = this.getAttribute('data-format');
        });
    });

    // Submit button
    const submitBtn = document.getElementById('submit-btn');
    if (submitBtn) {
        submitBtn.addEventListener('click', handleSubmit);
    }

    // Share button
    const shareBtn = document.getElementById('share-btn');
    if (shareBtn) {
        shareBtn.addEventListener('click', toggleShareMenu);
    }

    // Share options
    const shareOptions = document.querySelectorAll('.share-option');
    shareOptions.forEach(option => {
        option.addEventListener('click', function () {
            const action = this.getAttribute('data-action');
            handleShareAction(action);
        });
    });

    // Back to home button
    const backBtn = document.getElementById('back-to-home-btn');
    if (backBtn) {
        backBtn.addEventListener('click', showLandingPage);
    }

    // Close share menu when clicking outside
    document.addEventListener('click', function (event) {
        const shareContainer = document.querySelector('.share-container');
        const shareMenu = document.getElementById('share-menu');
        if (shareContainer && shareMenu && !shareContainer.contains(event.target)) {
            shareMenu.classList.remove('show');
        }
    });
}

// Load applications from API
async function loadApplications() {
    const listElement = document.getElementById('applications-list');
    if (!listElement) return;

    try {
        const response = await fetch('/Applications/GetApps');
        const result = await response.json();

        if (result.Status === 1 && result.Data && result.Data.length > 0) {
            listElement.innerHTML = '';
            result.Data.forEach(app => {
                const li = document.createElement('li');
                const link = document.createElement('a');
                link.href = '#';
                link.className = 'nav-link';
                link.innerHTML = `
                    <span class="nav-icon">${getAppIcon(app.AppId)}</span>
                    <span>${window.isArabic ? app.AppNameAr : app.AppNameEn}</span>
                `;
                link.addEventListener('click', function (e) {
                    e.preventDefault();
                    selectApplication(app);
                });
                li.appendChild(link);
                listElement.appendChild(li);
            });
        } else {
            listElement.innerHTML = `<li class="loading">${window.isArabic ? 'لا توجد تطبيقات' : 'No applications'}</li>`;
        }
    } catch (error) {
        console.error('Error loading applications:', error);
        listElement.innerHTML = `<li class="loading">${window.isArabic ? 'خطأ في التحميل' : 'Error loading'}</li>`;
    }
}

// Load recent requests from API
async function loadRecents() {
    const listElement = document.getElementById('recents-list');
    if (!listElement) return;

    try {
        const response = await fetch('/Recents/GetRecents');
        const result = await response.json();

        if (result.Status === 1 && result.Data && result.Data.length > 0) {
            listElement.innerHTML = '';
            result.Data.forEach(recent => {
                const li = document.createElement('li');
                const link = document.createElement('a');
                link.href = '#';
                link.className = 'nav-link';
                link.innerHTML = `
                    <span class="nav-icon">${getAppIcon(recent.AppId)}</span>
                    <span>${recent.Subject}</span>
                `;
                link.addEventListener('click', function (e) {
                    e.preventDefault();
                    loadRecentRequest(recent);
                });
                li.appendChild(link);
                listElement.appendChild(li);
            });
        } else {
            listElement.innerHTML = `<li class="loading">${window.isArabic ? 'لا توجد طلبات حديثة' : 'No recent requests'}</li>`;
        }
    } catch (error) {
        console.error('Error loading recents:', error);
        listElement.innerHTML = `<li class="loading">${window.isArabic ? 'خطأ في التحميل' : 'Error loading'}</li>`;
    }
}

// Select an application
function selectApplication(app) {
    selectedAppId = app.AppId;
    selectedAppName = window.isArabic ? app.AppNameAr : app.AppNameEn;

    // Update hidden fields
    document.getElementById('selected-appid').value = selectedAppId;
    document.getElementById('selected-appname').value = selectedAppName;

    // Update app header in form section
    const appNameElement = document.getElementById('selected-app-display-name');
    const appIconElement = document.getElementById('selected-app-icon');
    if (appNameElement) {
        appNameElement.textContent = selectedAppName;
    }
    if (appIconElement) {
        appIconElement.textContent = getAppIcon(app.AppId);
    }

    // Highlight selected app
    const allLinks = document.querySelectorAll('#applications-list .nav-link');
    allLinks.forEach(link => link.classList.remove('active'));
    event.target.closest('.nav-link').classList.add('active');

    // Show form section
    showFormSection();

    // Show success message after transition
    setTimeout(() => {
        showMessage('success', window.isArabic ?
            `تم اختيار التطبيق: ${selectedAppName}` :
            `Selected application: ${selectedAppName}`
        );
    }, 300);
}

// Load a recent request into the form
function loadRecentRequest(recent) {
    selectedAppId = recent.AppId;
    selectedAppName = window.isArabic ? recent.AppNameAr : recent.AppNameEn;

    // Update hidden fields
    document.getElementById('selected-appid').value = selectedAppId;
    document.getElementById('selected-appname').value = selectedAppName;

    // Update app header in form section
    const appNameElement = document.getElementById('selected-app-display-name');
    const appIconElement = document.getElementById('selected-app-icon');
    if (appNameElement) {
        appNameElement.textContent = selectedAppName;
    }
    if (appIconElement) {
        appIconElement.textContent = getAppIcon(recent.AppId);
    }

    // Fill form fields
    document.getElementById('subject').value = recent.Subject;
    document.getElementById('request-text').value = recent.Details;

    // Highlight selected recent
    const allLinks = document.querySelectorAll('#recents-list .nav-link');
    allLinks.forEach(link => link.classList.remove('active'));
    event.target.closest('.nav-link').classList.add('active');

    // Show form section
    showFormSection();

    // Show success message after transition
    setTimeout(() => {
        showMessage('success', window.isArabic ?
            'تم تحميل الطلب السابق' :
            'Previous request loaded'
        );
    }, 300);
}

// Handle form submission
async function handleSubmit() {
    const subject = document.getElementById('subject').value.trim();
    const requestText = document.getElementById('request-text').value.trim();

    // Validation
    if (!selectedAppId) {
        showMessage('error', window.isArabic ?
            'الرجاء اختيار تطبيق من القائمة الجانبية' :
            'Please select an application from the sidebar'
        );
        return;
    }

    if (!subject) {
        showMessage('error', window.isArabic ?
            'الرجاء إدخال الموضوع' :
            'Please enter a subject'
        );
        return;
    }

    if (!requestText) {
        showMessage('error', window.isArabic ?
            'الرجاء إدخال نص الطلب' :
            'Please enter request text'
        );
        return;
    }

    // Disable submit button
    const submitBtn = document.getElementById('submit-btn');
    submitBtn.disabled = true;
    submitBtn.textContent = window.isArabic ? 'جاري الإرسال...' : 'Submitting...';

    try {
        const payload = {
            Token: '',
            Username: '',
            AppId: selectedAppId,
            Subject: subject,
            RequestText: requestText
        };

        const response = await fetch('/Request/Submit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        const result = await response.json();

        if (result.Status === 1) {
            // Success
            lastResponseText = result.ResponseText;
            displayResponse(result.ResponseText);
            enableShareButton();
            showMessage('success', result.Message);

            // Reload recents to show the new request
            await loadRecents();
        } else {
            // Error
            showMessage('error', result.Message || (window.isArabic ?
                'فشل في إرسال الطلب' :
                'Failed to submit request'
            ));
        }
    } catch (error) {
        console.error('Error submitting request:', error);
        showMessage('error', window.isArabic ?
            'حدث خطأ أثناء إرسال الطلب' :
            'An error occurred while submitting the request'
        );
    } finally {
        // Re-enable submit button
        submitBtn.disabled = false;
        submitBtn.textContent = window.isArabic ? 'إرسال' : 'Submit';
    }
}

// Display response with Markdown formatting and chart rendering
function displayResponse(responseText) {
    const responseContainer = document.getElementById('response-container');
    const responseTextElement = document.getElementById('response-text');

    if (responseContainer && responseTextElement) {
        // Parse Markdown and convert to HTML
        if (typeof marked !== 'undefined') {
            // Configure marked options
            marked.setOptions({
                breaks: true,        // Convert \n to <br>
                gfm: true,          // GitHub Flavored Markdown
                tables: true,       // Support tables
                sanitize: false,    // Allow HTML (Gemini responses are safe)
                smartLists: true,   // Use smarter list behavior
                smartypants: true   // Use smart quotes and dashes
            });

            // Convert Markdown to HTML
            const htmlContent = marked.parse(responseText);
            responseTextElement.innerHTML = htmlContent;

            // Detect and render charts from the response
            detectAndRenderCharts(responseText, responseTextElement);
        } else {
            // Fallback to plain text if marked.js is not loaded
            responseTextElement.textContent = responseText;
        }

        responseContainer.style.display = 'block';
        responseContainer.scrollIntoView({ behavior: 'smooth' });
    }
}

// Detect and render charts from Gemini response
function detectAndRenderCharts(responseText, containerElement) {
    if (typeof ApexCharts === 'undefined') {
        return; // ApexCharts not loaded
    }

    // Try to detect chart data patterns
    const chartData = extractChartData(responseText);

    if (chartData) {
        renderChart(chartData, containerElement);
    }
}

// Extract chart data from response text
function extractChartData(text) {
    // Pattern 1: Look for JSON chart data in code blocks
    const jsonMatch = text.match(/```json\s*(\{[\s\S]*?\})\s*```/);
    if (jsonMatch) {
        try {
            const data = JSON.parse(jsonMatch[1]);
            if (data.chartType && data.labels && data.values) {
                return data;
            }
        } catch (e) {
            console.log('Failed to parse JSON chart data');
        }
    }

    // Pattern 2: Look for table data that could be a chart
    const tableMatch = text.match(/\|([^\n]+)\|\s*\n\|[-:\s|]+\|\s*\n((?:\|[^\n]+\|\s*\n?)+)/);
    if (tableMatch) {
        const headers = tableMatch[1].split('|').map(h => h.trim()).filter(h => h);
        const rows = tableMatch[2].trim().split('\n').map(row =>
            row.split('|').map(cell => cell.trim()).filter(cell => cell)
        );

        // Check if this looks like chart data (2 columns: label and value)
        if (headers.length === 2 && rows.length > 0) {
            const labels = rows.map(row => row[0]);
            const values = rows.map(row => parseFloat(row[1]) || 0);

            // Detect chart type from context
            const chartType = detectChartTypeFromContext(text);

            return {
                chartType: chartType,
                labels: labels,
                values: values,
                title: headers[0] + ' vs ' + headers[1]
            };
        }
    }

    return null;
}

// Detect chart type from context keywords
function detectChartTypeFromContext(text) {
    const lowerText = text.toLowerCase();

    if (lowerText.includes('pie chart') || lowerText.includes('pie')) {
        return 'pie';
    } else if (lowerText.includes('donut chart') || lowerText.includes('donut')) {
        return 'donut';
    } else if (lowerText.includes('line chart') || lowerText.includes('line graph')) {
        return 'line';
    } else if (lowerText.includes('area chart') || lowerText.includes('area')) {
        return 'area';
    } else if (lowerText.includes('bar chart') || lowerText.includes('horizontal bar')) {
        return 'bar';
    } else if (lowerText.includes('column chart') || lowerText.includes('column') || lowerText.includes('bar')) {
        return 'column';
    }

    // Default to column chart
    return 'column';
}

// Render chart using ApexCharts
function renderChart(chartData, containerElement) {
    // Create chart container
    const chartId = 'chart-' + Date.now();
    const chartContainer = document.createElement('div');
    chartContainer.id = chartId;
    chartContainer.className = 'apex-chart-container';
    containerElement.appendChild(chartContainer);

    // Prepare chart options based on type
    let options = {
        chart: {
            type: chartData.chartType === 'column' ? 'bar' : chartData.chartType,
            height: 400,
            toolbar: {
                show: true,
                tools: {
                    download: true,
                    selection: false,
                    zoom: false,
                    zoomin: false,
                    zoomout: false,
                    pan: false,
                    reset: false
                }
            },
            fontFamily: 'inherit'
        },
        title: {
            text: chartData.title || 'Chart',
            align: 'center',
            style: {
                fontSize: '18px',
                fontWeight: 'bold',
                color: '#333'
            }
        },
        theme: {
            palette: 'palette1'
        },
        colors: ['#667eea', '#764ba2', '#f093fb', '#4facfe', '#43e97b', '#fa709a', '#fee140', '#30cfd0']
    };

    // Configure based on chart type
    if (chartData.chartType === 'pie' || chartData.chartType === 'donut') {
        options.series = chartData.values;
        options.labels = chartData.labels;
        options.legend = {
            position: 'bottom'
        };
        options.dataLabels = {
            enabled: true,
            formatter: function (val) {
                return val.toFixed(1) + '%';
            }
        };
        if (chartData.chartType === 'donut') {
            options.plotOptions = {
                pie: {
                    donut: {
                        size: '65%'
                    }
                }
            };
        }
    } else if (chartData.chartType === 'line' || chartData.chartType === 'area') {
        options.series = [{
            name: chartData.title || 'Data',
            data: chartData.values
        }];
        options.xaxis = {
            categories: chartData.labels
        };
        options.stroke = {
            curve: 'smooth',
            width: 3
        };
        options.dataLabels = {
            enabled: false
        };
        if (chartData.chartType === 'area') {
            options.fill = {
                type: 'gradient',
                gradient: {
                    shadeIntensity: 1,
                    opacityFrom: 0.7,
                    opacityTo: 0.3
                }
            };
        }
    } else {
        // Bar or column chart
        options.series = [{
            name: chartData.title || 'Data',
            data: chartData.values
        }];
        options.xaxis = {
            categories: chartData.labels
        };
        options.plotOptions = {
            bar: {
                horizontal: chartData.chartType === 'bar',
                borderRadius: 4,
                dataLabels: {
                    position: 'top'
                }
            }
        };
        options.dataLabels = {
            enabled: true,
            offsetY: chartData.chartType === 'bar' ? 0 : -20,
            style: {
                fontSize: '12px',
                colors: ['#333']
            }
        };
    }

    // Render the chart
    const chart = new ApexCharts(document.getElementById(chartId), options);
    chart.render();
}

// Enable share button
function enableShareButton() {
    const shareBtn = document.getElementById('share-btn');
    if (shareBtn) {
        shareBtn.disabled = false;
    }
}

// Toggle share menu
function toggleShareMenu(event) {
    event.stopPropagation();
    const shareMenu = document.getElementById('share-menu');
    if (shareMenu) {
        shareMenu.classList.toggle('show');
    }
}

// Handle share actions
function handleShareAction(action) {
    const shareMenu = document.getElementById('share-menu');
    if (shareMenu) {
        shareMenu.classList.remove('show');
    }

    if (!lastResponseText) {
        showMessage('error', window.isArabic ?
            'لا يوجد محتوى للمشاركة' :
            'No content to share'
        );
        return;
    }

    switch (action) {
        case 'email':
            handleEmailShare();
            break;
        case 'download':
            handleDownload();
            break;
        case 'print':
            handlePrint();
            break;
    }
}

// Handle email share
function handleEmailShare() {
    const subject = document.getElementById('subject').value;
    const body = lastResponseText;
    const mailto = `mailto:?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
    window.location.href = mailto;
}

// Handle download
function handleDownload() {
    const subject = document.getElementById('subject').value;
    const content = lastResponseText;
    const blob = new Blob([content], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${subject}.txt`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);

    showMessage('success', window.isArabic ?
        'تم تحميل الملف' :
        'File downloaded'
    );
}

// Handle print
function handlePrint() {
    const printWindow = window.open('', '_blank');
    const subject = document.getElementById('subject').value;
    const content = lastResponseText;

    printWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>${subject}</title>
            <style>
                body {
                    font-family: Arial, sans-serif;
                    padding: 20px;
                    line-height: 1.6;
                }
                h1 {
                    color: #333;
                    margin-bottom: 20px;
                }
                .content {
                    white-space: pre-wrap;
                }
            </style>
        </head>
        <body>
            <h1>${subject}</h1>
            <div class="content">${content}</div>
        </body>
        </html>
    `);
    printWindow.document.close();
    printWindow.focus();
    setTimeout(() => {
        printWindow.print();
    }, 250);
}

// Show message
function showMessage(type, message) {
    // Remove existing message
    const existingMessage = document.querySelector('.message');
    if (existingMessage) {
        existingMessage.remove();
    }

    // Create new message
    const messageDiv = document.createElement('div');
    messageDiv.className = `message message-${type}`;
    messageDiv.textContent = message;

    // Insert at top of form
    const form = document.querySelector('.request-form');
    if (form) {
        form.insertBefore(messageDiv, form.firstChild);

        // Auto-remove after 5 seconds
        setTimeout(() => {
            messageDiv.remove();
        }, 5000);
    }
}

// Get app icon based on app ID
function getAppIcon(appId) {
    const icons = {
        1: '📊',
        2: '👥',
        3: '💰',
        4: '📦',
        5: '👤'
    };
    return icons[appId] || '📄';
}
