// =======================
// AI Reports Tool - Main JavaScript
// Handles all client-side interactions
// =======================

// Global state
let selectedAppId = null;
let selectedAppName = null;
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

    document.getElementById('selected-appid').value = '';
    document.getElementById('selected-appname').value = '';
    document.getElementById('subject').value = '';
    document.getElementById('request-text').value = '';
    document.getElementById('response-container').style.display = 'none';
    document.getElementById('share-btn').disabled = true;
}

// Setup all event listeners
function setupEventListeners() {
    // Submit button
    const submitBtn = document.getElementById('submit-btn');
    if (submitBtn) {
        submitBtn.addEventListener('click', handleSubmit);
    }

    // Get Query button
    const getQueryBtn = document.getElementById('get-query-btn');
    if (getQueryBtn) {
        getQueryBtn.addEventListener('click', handleGetQuery);
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
                li.className = 'recent-item';

                const link = document.createElement('a');
                link.href = '#';
                link.className = 'nav-link';
                link.innerHTML = `
                    <span class="nav-icon">${getAppIcon(recent.AppId)}</span>
                    <span class="recent-subject">${recent.Subject}</span>
                `;
                link.addEventListener('click', function (e) {
                    e.preventDefault();
                    loadRecentRequest(recent);
                });

                // Add delete button
                const deleteBtn = document.createElement('button');
                deleteBtn.className = 'delete-recent-btn';
                deleteBtn.innerHTML = '🗑️';
                deleteBtn.title = window.isArabic ? 'حذف' : 'Delete';
                deleteBtn.setAttribute('data-request-id', recent.RequestId);
                deleteBtn.addEventListener('click', function (e) {
                    e.stopPropagation();
                    confirmDeleteRecent(recent.RequestId, recent.Subject);
                });

                li.appendChild(link);
                li.appendChild(deleteBtn);
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

// Confirm delete recent request
function confirmDeleteRecent(requestId, subject) {
    const message = window.isArabic
        ? `هل أنت متأكد من حذف هذا الطلب؟\n\n"${subject}"`
        : `Are you sure you want to delete this request?\n\n"${subject}"`;

    if (confirm(message)) {
        deleteRecentRequest(requestId);
    }
}

// Delete recent request
async function deleteRecentRequest(requestId) {
    try {
        const response = await fetch(`/Request/Delete/${requestId}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (result.Status === 1) {
            showMessage('success', window.isArabic ?
                'تم حذف الطلب بنجاح' :
                'Request deleted successfully'
            );
            // Reload recents list
            await loadRecents();
        } else {
            showMessage('error', result.Message || (window.isArabic ?
                'فشل حذف الطلب' :
                'Failed to delete request'
            ));
        }
    } catch (error) {
        console.error('Error deleting request:', error);
        showMessage('error', window.isArabic ?
            'حدث خطأ أثناء حذف الطلب' :
            'An error occurred while deleting the request'
        );
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
}

// Show loading overlay and disable form elements
function showLoading() {
    const loadingOverlay = document.getElementById('loading-overlay');
    const requestForm = document.querySelector('.request-form');

    if (loadingOverlay) {
        loadingOverlay.style.display = 'flex';
    }

    if (requestForm) {
        requestForm.classList.add('loading-disabled');
    }

    // Disable all form inputs and buttons
    const inputs = document.querySelectorAll('#subject, #request-text');
    const buttons = document.querySelectorAll('#submit-btn, #share-btn');

    inputs.forEach(input => input.disabled = true);
    buttons.forEach(button => button.disabled = true);
}

// Hide loading overlay and enable form elements
function hideLoading() {
    const loadingOverlay = document.getElementById('loading-overlay');
    const requestForm = document.querySelector('.request-form');

    if (loadingOverlay) {
        loadingOverlay.style.display = 'none';
    }

    if (requestForm) {
        requestForm.classList.remove('loading-disabled');
    }

    // Re-enable all form inputs and buttons
    const inputs = document.querySelectorAll('#subject, #request-text');
    const buttons = document.querySelectorAll('#submit-btn');

    inputs.forEach(input => input.disabled = false);
    buttons.forEach(button => button.disabled = false);
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

    // Show loading overlay and disable form
    showLoading();

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
            displayResponse(result.ResponseText, true); // true = remove code blocks
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
        // Hide loading overlay and re-enable form
        hideLoading();
    }
}

// Handle Get Query button click
async function handleGetQuery() {
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

    // Show loading overlay and disable form
    showLoading();

    try {
        const payload = {
            Token: '',
            Username: '',
            AppId: selectedAppId,
            Subject: subject,
            RequestText: requestText
        };

        const response = await fetch('/Request/GenerateQuery', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        const result = await response.json();

        if (result.Status === 1) {
            // Success - display the markdown report generated by Gemini
            displayResponse(result.ResponseText, true);

            lastResponseText = result.ResponseText;
            enableShareButton();
            showMessage('success', result.Message);
        } else {
            // Error
            showMessage('error', result.Message || (window.isArabic ?
                'فشل في إنشاء الاستعلام' :
                'Failed to generate query'
            ));
        }
    } catch (error) {
        console.error('Error generating query:', error);
        showMessage('error', window.isArabic ?
            'حدث خطأ أثناء إنشاء الاستعلام' :
            'An error occurred while generating the query'
        );
    } finally {
        // Hide loading overlay and re-enable form
        hideLoading();
    }
}

// Display response with Markdown formatting and chart rendering
function displayResponse(responseText, removeCodeBlocks = false) {
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

            // Clean the text based on options
            let cleanedText = responseText;

            // Remove Gemini's meta-messages (introductory and concluding statements)
            // Remove lines like "Okay, I will assist you with..."
            cleanedText = cleanedText.replace(/^Okay,?\s+I\s+will\s+assist\s+you.*?\.(\s*\n)*/im, '');
            cleanedText = cleanedText.replace(/^Sure,?\s+I\s+will\s+help.*?\.(\s*\n)*/im, '');
            cleanedText = cleanedText.replace(/^Sure,?\s+I\s+can\s+help.*?\.(\s*\n)*/im, '');
            cleanedText = cleanedText.replace(/^I\s+will\s+assist.*?\.(\s*\n)*/im, '');
            cleanedText = cleanedText.replace(/^I\s+will\s+help.*?\.(\s*\n)*/im, '');

            // Remove lines that reference the application name
            cleanedText = cleanedText.replace(/with\s+the\s+['"'].*?['"']\s+application.*?\.(\s*\n)*/gi, '');

            // Remove "Here is..." introductory lines
            cleanedText = cleanedText.replace(/^Here\s+is\s+.*?:(\s*\n)*/im, '');
            cleanedText = cleanedText.replace(/^Here\s+are\s+.*?:(\s*\n)*/im, '');
            cleanedText = cleanedText.replace(/^Below\s+is\s+.*?:(\s*\n)*/im, '');
            cleanedText = cleanedText.replace(/^Below\s+are\s+.*?:(\s*\n)*/im, '');

            // Remove concluding statements like "I hope this helps"
            cleanedText = cleanedText.replace(/I\s+hope\s+this\s+helps.*?\.?$/im, '');
            cleanedText = cleanedText.replace(/Please\s+let\s+me\s+know\s+if.*?\.?$/im, '');
            cleanedText = cleanedText.replace(/Let\s+me\s+know\s+if.*?\.?$/im, '');
            cleanedText = cleanedText.replace(/Feel\s+free\s+to\s+ask.*?\.?$/im, '');

            // Only remove code blocks if requested (for regular Submit, not for Get Query)
            if (removeCodeBlocks) {
                // Remove all code blocks (```...```) regardless of language
                cleanedText = cleanedText.replace(/```[\s\S]*?```/g, '');

                // Remove any lines that introduce code/data structures
                cleanedText = cleanedText.replace(/JSON\/Programmatic Data Structure.*?:\s*/gi, '');
                cleanedText = cleanedText.replace(/Example \(for.*?\):\s*/gi, '');
                cleanedText = cleanedText.replace(/Code Example.*?:\s*/gi, '');
                cleanedText = cleanedText.replace(/Sample Code.*?:\s*/gi, '');
            }

            // Clean up multiple consecutive newlines
            cleanedText = cleanedText.replace(/\n{3,}/g, '\n\n');

            // Trim leading and trailing whitespace
            cleanedText = cleanedText.trim();

            // Convert Markdown to HTML
            const htmlContent = marked.parse(cleanedText);
            responseTextElement.innerHTML = htmlContent;

            // Detect and render charts from the ORIGINAL response text (not cleaned)
            detectAndRenderCharts(responseText, responseTextElement);
        } else {
            // Fallback to plain text if marked.js is not loaded
            responseTextElement.textContent = responseText;
        }

        responseContainer.style.display = 'block';
        responseContainer.scrollIntoView({ behavior: 'smooth' });
    }
}

// Display query results with summary and data table
function displayQueryResults(data) {
    const responseContainer = document.getElementById('response-container');
    const responseTextElement = document.getElementById('response-text');

    if (responseContainer && responseTextElement) {
        let html = '';

        // Display summary if available
        if (data.Summary && data.Summary.trim()) {
            html += `<div class="query-summary">
                <h3>${window.isArabic ? 'الملخص' : 'Summary'}</h3>
                <p>${escapeHtml(data.Summary)}</p>
            </div>`;
        }

        // Display results table
        if (data.Results && data.Results.length > 0) {
            html += `<div class="query-results">
                <h3>${window.isArabic ? 'النتائج' : 'Results'} (${data.Results.length} ${window.isArabic ? 'صف' : 'rows'})</h3>
                ${buildResultsTable(data.Results)}
            </div>`;
        } else {
            html += `<div class="query-results">
                <p class="no-results">${window.isArabic ? 'لا توجد نتائج' : 'No results found'}</p>
            </div>`;
        }

        responseTextElement.innerHTML = html;
        responseContainer.style.display = 'block';
        responseContainer.scrollIntoView({ behavior: 'smooth' });
    }
}

// Build HTML table from query results
function buildResultsTable(results) {
    if (!results || results.length === 0) {
        return '<p>No data</p>';
    }

    // Get column names from first row
    const columns = Object.keys(results[0]);

    let html = '<div class="table-responsive"><table class="results-table">';

    // Table header
    html += '<thead><tr>';
    columns.forEach(column => {
        html += `<th>${escapeHtml(column)}</th>`;
    });
    html += '</tr></thead>';

    // Table body
    html += '<tbody>';
    results.forEach(row => {
        html += '<tr>';
        columns.forEach(column => {
            const value = row[column];
            const displayValue = value === null || value === undefined ? '<span class="null-value">NULL</span>' : escapeHtml(String(value));
            html += `<td>${displayValue}</td>`;
        });
        html += '</tr>';
    });
    html += '</tbody>';

    html += '</table></div>';
    return html;
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Detect and render charts from Gemini response
function detectAndRenderCharts(responseText, containerElement) {
    if (typeof Chart === 'undefined') {
        console.log('Chart.js library not loaded');
        return;
    }

    console.log('Detecting charts in response...');

    // Try to detect chart data patterns from raw text
    const chartData = extractChartData(responseText);

    if (chartData) {
        console.log('Chart data found from text:', chartData);
        renderChart(chartData, containerElement);
        return;
    }

    // Try to detect chart data from rendered HTML tables
    const tableChartData = extractChartDataFromTables(containerElement, responseText);
    if (tableChartData) {
        // Check if it's an array of charts or a single chart
        if (Array.isArray(tableChartData)) {
            console.log(`Chart data found from tables: ${tableChartData.length} chart(s) to render`);
            // Render all charts
            tableChartData.forEach((chartData, index) => {
                console.log(`Rendering chart ${index + 1}/${tableChartData.length}:`, chartData);
                renderChart(chartData, containerElement);
            });
        } else {
            console.log('Chart data found from table:', tableChartData);
            renderChart(tableChartData, containerElement);
        }
        return;
    }

    console.log('No chart data detected');
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
function detectChartTypeFromContext(localContext, fallbackText = '') {
    // First try to detect from local context (heading/text near the table)
    // Then fall back to the full text if nothing found locally
    const localLower = localContext.toLowerCase();
    const fallbackLower = fallbackText.toLowerCase();

    // Check local context first (prioritize specific table context)
    if (localLower.includes('pie chart') || localLower.includes('pie')) {
        return 'pie';
    } else if (localLower.includes('donut chart') || localLower.includes('donut')) {
        return 'donut';
    } else if (localLower.includes('line chart') || localLower.includes('line graph')) {
        return 'line';
    } else if (localLower.includes('area chart') || localLower.includes('area')) {
        return 'area';
    } else if (localLower.includes('bar chart') || localLower.includes('horizontal bar')) {
        return 'bar';
    } else if (localLower.includes('column chart') || localLower.includes('column')) {
        return 'column';
    }

    // If no specific type found in local context, check fallback text
    if (fallbackText) {
        if (fallbackLower.includes('pie chart') || fallbackLower.includes('pie')) {
            return 'pie';
        } else if (fallbackLower.includes('donut chart') || fallbackLower.includes('donut')) {
            return 'donut';
        } else if (fallbackLower.includes('line chart') || fallbackLower.includes('line graph')) {
            return 'line';
        } else if (fallbackLower.includes('area chart') || fallbackLower.includes('area')) {
            return 'area';
        } else if (fallbackLower.includes('bar chart') || fallbackLower.includes('horizontal bar')) {
            return 'bar';
        } else if (fallbackLower.includes('column chart') || fallbackLower.includes('column') || fallbackLower.includes('bar')) {
            return 'column';
        }
    }

    // Default to column chart
    return 'column';
}

// Helper function to get text context before a table (headings, paragraphs)
function getTableContext(table) {
    let context = '';
    let currentElement = table.previousElementSibling;
    let elementsChecked = 0;
    const maxElementsToCheck = 3; // Look at up to 3 elements before the table

    while (currentElement && elementsChecked < maxElementsToCheck) {
        const tagName = currentElement.tagName.toLowerCase();

        // Include headings (h1-h6) and paragraphs
        if (tagName.match(/^h[1-6]$/) || tagName === 'p') {
            context = currentElement.textContent + ' ' + context;
        }

        currentElement = currentElement.previousElementSibling;
        elementsChecked++;
    }

    return context.trim();
}

// Helper function to check if table context indicates it should be a chart
function shouldTableBeChart(tableContext, userRequest) {
    const combinedText = (tableContext + ' ' + userRequest).toLowerCase();

    // Check for chart-related keywords
    const chartKeywords = ['chart', 'graph', 'pie', 'donut', 'bar', 'column', 'line', 'area', 'visualization', 'visualize'];

    return chartKeywords.some(keyword => combinedText.includes(keyword));
}

// Extract chart data from rendered HTML tables
function extractChartDataFromTables(containerElement, originalText) {
    console.log('Checking for tables to convert to charts...');

    // Find all tables in the container
    const tables = containerElement.querySelectorAll('table');
    console.log('Tables found:', tables.length);

    if (tables.length === 0) {
        console.log('No tables found');
        return null;
    }

    // Extract data from ALL tables (not just the first one)
    const allChartsData = [];

    tables.forEach((table, tableIndex) => {
        console.log(`Processing table ${tableIndex + 1}/${tables.length}`);

        // Get context around this specific table
        const tableContext = getTableContext(table);
        console.log(`Table ${tableIndex + 1} context: "${tableContext}"`);

        // Check if this table should be rendered as a chart
        if (!shouldTableBeChart(tableContext, originalText)) {
            console.log(`Table ${tableIndex + 1}: No chart keywords in context, skipping`);
            return; // Skip this table
        }

        const rows = table.querySelectorAll('tbody tr');
        console.log('Table rows found:', rows.length);

        let labels = [];
        let values = [];
        let hasNumericData = false;

        if (rows.length === 0) {
            // Try without tbody
            const allRows = table.querySelectorAll('tr');
            console.log('Total rows (without tbody):', allRows.length);

            if (allRows.length === 0) {
                console.log(`Table ${tableIndex + 1}: No rows found, skipping`);
                return; // Skip this table, continue to next
            }

            // Extract data from rows (skip header row if exists)
            let startIndex = 0;

            // Check if first row is a header
            const firstRow = allRows[0];
            const firstRowCells = firstRow.querySelectorAll('th');
            if (firstRowCells.length > 0) {
                startIndex = 1; // Skip header row
            }

            for (let i = startIndex; i < allRows.length; i++) {
                const row = allRows[i];
                const cells = row.querySelectorAll('td, th');

                if (cells.length >= 2) {
                    const label = cells[0].textContent.trim();
                    const valueText = cells[1].textContent.trim();

                    // Try to parse as number (remove commas, currency symbols, etc.)
                    const cleanValue = valueText.replace(/[,%$€£¥]/g, '').trim();
                    const numericValue = parseFloat(cleanValue);

                    console.log(`Table ${tableIndex + 1}, Row ${i}: "${label}" = "${valueText}" -> ${numericValue}`);

                    if (!isNaN(numericValue)) {
                        labels.push(label);
                        values.push(numericValue);
                        hasNumericData = true;
                    }
                }
            }

            if (!hasNumericData || labels.length === 0) {
                console.log(`Table ${tableIndex + 1}: No numeric data found, skipping`);
                return; // Skip this table, continue to next
            }

            // Get headers for title
            const headers = table.querySelectorAll('th');
            let title = 'Data Visualization';
            if (headers.length >= 2) {
                title = `${headers[0].textContent.trim()} vs ${headers[1].textContent.trim()}`;
            }

            // Detect chart type based on THIS table's context first, then fall back to full text
            const chartType = detectChartTypeFromContext(tableContext, originalText);

            console.log(`Table ${tableIndex + 1}: Chart data extracted:`, { chartType, labels, values, title });

            // Add to collection
            allChartsData.push({
                chartType: chartType,
                labels: labels,
                values: values,
                title: title
            });
            return; // Move to next table
        }

        // Logic for tables with tbody
        rows.forEach((row, index) => {
            const cells = row.querySelectorAll('td');
            if (cells.length >= 2) {
                const label = cells[0].textContent.trim();
                const valueText = cells[1].textContent.trim();

                // Try to parse as number (remove commas, currency symbols, etc.)
                const cleanValue = valueText.replace(/[,%$€£¥]/g, '').trim();
                const numericValue = parseFloat(cleanValue);

                console.log(`Table ${tableIndex + 1}, Row ${index}: "${label}" = "${valueText}" -> ${numericValue}`);

                if (!isNaN(numericValue)) {
                    labels.push(label);
                    values.push(numericValue);
                    hasNumericData = true;
                }
            }
        });

        if (!hasNumericData || labels.length === 0) {
            console.log(`Table ${tableIndex + 1}: No numeric data found, skipping`);
            return; // Skip this table, continue to next
        }

        // Get headers for title
        const headers = table.querySelectorAll('thead th');
        let title = 'Data Visualization';
        if (headers.length >= 2) {
            title = `${headers[0].textContent.trim()} vs ${headers[1].textContent.trim()}`;
        }

        // Detect chart type based on THIS table's context first, then fall back to full text
        const chartType = detectChartTypeFromContext(tableContext, originalText);

        console.log(`Table ${tableIndex + 1}: Chart data extracted:`, { chartType, labels, values, title });

        // Add to collection
        allChartsData.push({
            chartType: chartType,
            labels: labels,
            values: values,
            title: title
        });
    });

    // Return all charts data or null if none found
    if (allChartsData.length === 0) {
        console.log('No chart data found in any table');
        return null;
    }

    console.log(`Total charts to render: ${allChartsData.length}`);
    return allChartsData;
}

// Render chart using Chart.js
function renderChart(chartData, containerElement) {
    try {
        console.log('Rendering chart with Chart.js:', chartData);

        // Create chart container div
        const chartWrapper = document.createElement('div');
        chartWrapper.className = 'chart-container';

        // Add title
        const chartTitle = document.createElement('h3');
        chartTitle.textContent = chartData.title || 'Chart';
        chartTitle.style.textAlign = 'center';
        chartTitle.style.marginBottom = '1rem';
        chartTitle.style.fontSize = '1.25rem';
        chartTitle.style.fontWeight = '600';
        chartTitle.style.color = '#333';
        chartWrapper.appendChild(chartTitle);

        // Create canvas element
        const canvas = document.createElement('canvas');
        const chartId = 'chart-' + Date.now();
        canvas.id = chartId;
        chartWrapper.appendChild(canvas);

        // Append to container
        containerElement.appendChild(chartWrapper);

        // Chart colors
        const colors = ['#667eea', '#764ba2', '#f093fb', '#4facfe', '#43e97b', '#fa709a', '#fee140', '#30cfd0'];

        // Prepare Chart.js configuration
        let chartType = chartData.chartType;
        let config = {
            type: chartType,
            data: {
                labels: chartData.labels,
                datasets: [{
                    label: chartData.title || 'Data',
                    data: chartData.values,
                    backgroundColor: colors,
                    borderColor: colors,
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: true,
                        position: 'bottom'
                    },
                    tooltip: {
                        enabled: true
                    }
                }
            }
        };

        // Customize based on chart type
        if (chartType === 'pie' || chartType === 'doughnut') {
            // For pie/doughnut charts, use multiple colors
            config.data.datasets[0].backgroundColor = colors.slice(0, chartData.values.length);
            config.data.datasets[0].borderColor = '#fff';
            config.data.datasets[0].borderWidth = 2;

            // Map 'donut' to 'doughnut' for Chart.js
            if (chartType === 'donut') {
                config.type = 'doughnut';
            }
        } else if (chartType === 'line' || chartType === 'area') {
            // Line chart configuration
            config.data.datasets[0].backgroundColor = chartType === 'area' ? 'rgba(102, 126, 234, 0.2)' : 'transparent';
            config.data.datasets[0].borderColor = '#667eea';
            config.data.datasets[0].fill = chartType === 'area';
            config.data.datasets[0].tension = 0.4; // Smooth curves
            config.options.scales = {
                y: {
                    beginAtZero: true
                }
            };

            // Map 'area' to 'line' with fill
            if (chartType === 'area') {
                config.type = 'line';
            }
        } else if (chartType === 'bar' || chartType === 'column') {
            // Bar/column chart configuration
            config.data.datasets[0].backgroundColor = '#667eea';
            config.data.datasets[0].borderColor = '#667eea';

            // Map 'column' to vertical 'bar' in Chart.js
            if (chartType === 'column') {
                config.type = 'bar';
                config.options.indexAxis = 'x'; // Vertical bars
            } else {
                config.type = 'bar';
                config.options.indexAxis = 'y'; // Horizontal bars
            }

            config.options.scales = {
                y: {
                    beginAtZero: true
                }
            };
        }

        // Create the chart
        const ctx = canvas.getContext('2d');
        new Chart(ctx, config);

        console.log('Chart rendered successfully with Chart.js');
    } catch (error) {
        console.error('Error rendering chart:', error);
    }
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
        case 'print':
            handlePrint();
            break;
        case 'pdf':
            handleSavePDF();
            break;
        case 'word':
            handleSaveWord();
            break;
    }
}

// Convert canvas charts to images for export (captures from original, replaces in clone)
function captureChartsAsImages(originalElement, clonedElement) {
    const originalCanvases = originalElement.querySelectorAll('.chart-container canvas');
    const clonedCanvases = clonedElement.querySelectorAll('.chart-container canvas');

    console.log('Original canvases found:', originalCanvases.length);
    console.log('Cloned canvases found:', clonedCanvases.length);

    originalCanvases.forEach((originalCanvas, index) => {
        try {
            if (index < clonedCanvases.length) {
                const clonedCanvas = clonedCanvases[index];

                // Convert original canvas to data URL (image)
                const imageUrl = originalCanvas.toDataURL('image/png');
                console.log('Captured chart image:', imageUrl.substring(0, 50) + '...');

                // Create img element
                const img = document.createElement('img');
                img.src = imageUrl;
                img.style.maxWidth = '100%';
                img.style.height = 'auto';
                img.style.display = 'block';
                img.style.margin = '0 auto';

                // Replace canvas with img in the cloned element
                clonedCanvas.parentNode.replaceChild(img, clonedCanvas);
                console.log('Replaced canvas with image in clone');
            }
        } catch (error) {
            console.error('Error converting chart to image:', error);
        }
    });
}

// Handle print - prints the formatted response with all styling
function handlePrint() {
    const subject = document.getElementById('subject').value;
    const responseTextElement = document.getElementById('response-text');

    if (!responseTextElement) {
        showMessage('error', window.isArabic ?
            'لا يوجد محتوى للطباعة' :
            'No content to print'
        );
        return;
    }

    // Clone the response element to avoid modifying the original
    const clonedElement = responseTextElement.cloneNode(true);

    // Capture charts from original and replace in clone
    captureChartsAsImages(responseTextElement, clonedElement);

    const printWindow = window.open('', '_blank');
    const htmlContent = clonedElement.innerHTML;
    const isRtl = window.isArabic;

    printWindow.document.write(`
        <!DOCTYPE html>
        <html dir="${isRtl ? 'rtl' : 'ltr'}" lang="${isRtl ? 'ar' : 'en'}">
        <head>
            <meta charset="UTF-8">
            <title>${subject}</title>
            <style>
                body {
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
                    padding: 40px;
                    line-height: 1.6;
                    color: #333;
                    direction: ${isRtl ? 'rtl' : 'ltr'};
                }
                h1 {
                    color: #333;
                    margin-bottom: 30px;
                    font-size: 2rem;
                    border-bottom: 2px solid #667eea;
                    padding-bottom: 10px;
                }
                h2, h3, h4, h5, h6 {
                    color: #333;
                    margin-top: 1.5rem;
                    margin-bottom: 0.75rem;
                }
                table {
                    border-collapse: collapse;
                    width: 100%;
                    margin: 1rem 0;
                }
                th, td {
                    border: 1px solid #ddd;
                    padding: 0.75rem;
                    text-align: ${isRtl ? 'right' : 'left'};
                }
                th {
                    background-color: #667eea;
                    color: white;
                    font-weight: 600;
                }
                tr:nth-child(even) {
                    background-color: #f9fafb;
                }
                code {
                    background-color: #f3f4f6;
                    padding: 0.2rem 0.4rem;
                    border-radius: 3px;
                    font-family: 'Courier New', monospace;
                    font-size: 0.9em;
                }
                pre {
                    background-color: #1e1e1e;
                    color: #d4d4d4;
                    padding: 1rem;
                    border-radius: 6px;
                    overflow-x: auto;
                    margin: 1rem 0;
                }
                pre code {
                    background: none;
                    padding: 0;
                    color: inherit;
                }
                ul, ol {
                    margin-bottom: 1rem;
                    padding-${isRtl ? 'right' : 'left'}: 2rem;
                }
                blockquote {
                    border-${isRtl ? 'right' : 'left'}: 4px solid #667eea;
                    margin: 1rem 0;
                    padding: 0.5rem 1rem;
                    background-color: #f9fafb;
                }
                img {
                    max-width: 100%;
                    height: auto;
                }
                .chart-container {
                    margin: 2rem 0;
                    page-break-inside: avoid;
                }
                @media print {
                    body {
                        padding: 20px;
                    }
                    .chart-container {
                        break-inside: avoid;
                    }
                }
            </style>
        </head>
        <body>
            <h1>${subject}</h1>
            <div class="content">${htmlContent}</div>
        </body>
        </html>
    `);
    printWindow.document.close();
    printWindow.focus();

    // Wait for content to load before printing
    setTimeout(() => {
        printWindow.print();
    }, 500);
}

// Handle Save PDF - saves the formatted response as PDF
async function handleSavePDF() {
    const subject = document.getElementById('subject').value;
    const responseTextElement = document.getElementById('response-text');

    if (!responseTextElement) {
        showMessage('error', window.isArabic ?
            'لا يوجد محتوى لحفظه' :
            'No content to save'
        );
        return;
    }

    // Check if html2pdf is loaded
    if (typeof html2pdf === 'undefined') {
        showMessage('error', window.isArabic ?
            'مكتبة PDF غير محملة' :
            'PDF library not loaded'
        );
        return;
    }

    try {
        // Show loading message
        showMessage('success', window.isArabic ?
            'جاري إنشاء ملف PDF...' :
            'Generating PDF...'
        );

        // Create a container for PDF content
        const pdfContainer = document.createElement('div');
        pdfContainer.style.padding = '40px';
        pdfContainer.style.backgroundColor = 'white';
        pdfContainer.style.direction = window.isArabic ? 'rtl' : 'ltr';

        // Add title
        const titleElement = document.createElement('h1');
        titleElement.textContent = subject;
        titleElement.style.marginBottom = '20px';
        titleElement.style.borderBottom = '2px solid #667eea';
        titleElement.style.paddingBottom = '10px';
        pdfContainer.appendChild(titleElement);

        // Clone the response content
        const contentClone = responseTextElement.cloneNode(true);

        // Capture charts from original and replace in clone
        captureChartsAsImages(responseTextElement, contentClone);

        pdfContainer.appendChild(contentClone);

        // PDF options
        const options = {
            margin: [15, 15, 15, 15],
            filename: `${subject}.pdf`,
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: {
                scale: 2,
                useCORS: true,
                logging: false
            },
            jsPDF: {
                unit: 'mm',
                format: 'a4',
                orientation: 'portrait'
            }
        };

        // Generate and save PDF
        await html2pdf().set(options).from(pdfContainer).save();

        showMessage('success', window.isArabic ?
            'تم حفظ ملف PDF' :
            'PDF saved successfully'
        );
    } catch (error) {
        console.error('Error generating PDF:', error);
        showMessage('error', window.isArabic ?
            'حدث خطأ أثناء إنشاء PDF' :
            'Error generating PDF'
        );
    }
}

// Handle Save Word - saves the formatted response as Word document
function handleSaveWord() {
    const subject = document.getElementById('subject').value;
    const responseTextElement = document.getElementById('response-text');

    if (!responseTextElement) {
        showMessage('error', window.isArabic ?
            'لا يوجد محتوى لحفظه' :
            'No content to save'
        );
        return;
    }

    try {
        // Clone the response element to avoid modifying the original
        const clonedElement = responseTextElement.cloneNode(true);

        // Capture charts from original and replace in clone
        captureChartsAsImages(responseTextElement, clonedElement);

        const htmlContent = clonedElement.innerHTML;
        const isRtl = window.isArabic;

        // Create a complete HTML document for Word
        const wordContent = `
            <!DOCTYPE html>
            <html xmlns:o="urn:schemas-microsoft-com:office:office"
                  xmlns:w="urn:schemas-microsoft-com:office:word"
                  xmlns="http://www.w3.org/TR/REC-html40">
            <head>
                <meta charset="utf-8">
                <title>${subject}</title>
                <style>
                    body {
                        font-family: Arial, sans-serif;
                        line-height: 1.6;
                        direction: ${isRtl ? 'rtl' : 'ltr'};
                        padding: 40px;
                    }
                    h1 {
                        color: #333;
                        margin-bottom: 20px;
                        border-bottom: 2px solid #667eea;
                        padding-bottom: 10px;
                        font-size: 24pt;
                    }
                    h2 { font-size: 18pt; margin-top: 20px; }
                    h3 { font-size: 14pt; margin-top: 16px; }
                    h4 { font-size: 12pt; margin-top: 14px; }
                    table {
                        border-collapse: collapse;
                        width: 100%;
                        margin: 10px 0;
                    }
                    th, td {
                        border: 1px solid #ddd;
                        padding: 8px;
                        text-align: ${isRtl ? 'right' : 'left'};
                    }
                    th {
                        background-color: #667eea;
                        color: white;
                        font-weight: bold;
                    }
                    tr:nth-child(even) {
                        background-color: #f9fafb;
                    }
                    code {
                        background-color: #f3f4f6;
                        padding: 2px 4px;
                        border-radius: 3px;
                        font-family: Courier New, monospace;
                    }
                    pre {
                        background-color: #f5f5f5;
                        padding: 10px;
                        border: 1px solid #ddd;
                        border-radius: 4px;
                        overflow-x: auto;
                    }
                    pre code {
                        background: none;
                        padding: 0;
                    }
                    ul, ol {
                        margin: 10px 0;
                        padding-${isRtl ? 'right' : 'left'}: 30px;
                    }
                    blockquote {
                        border-${isRtl ? 'right' : 'left'}: 4px solid #667eea;
                        margin: 10px 0;
                        padding: 5px 10px;
                        background-color: #f9fafb;
                    }
                    img {
                        max-width: 100%;
                        height: auto;
                    }
                </style>
            </head>
            <body>
                <h1>${subject}</h1>
                ${htmlContent}
            </body>
            </html>
        `;

        // Create blob with Word MIME type
        const blob = new Blob(['\ufeff', wordContent], {
            type: 'application/msword'
        });

        // Create download link
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${subject}.doc`;

        // Trigger download
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        // Clean up
        URL.revokeObjectURL(url);

        showMessage('success', window.isArabic ?
            'تم حفظ ملف Word' :
            'Word document saved successfully'
        );
    } catch (error) {
        console.error('Error generating Word document:', error);
        showMessage('error', window.isArabic ?
            'حدث خطأ أثناء إنشاء ملف Word' :
            'Error generating Word document'
        );
    }
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
